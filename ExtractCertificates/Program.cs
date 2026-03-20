using System.Collections;
using System.Formats.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Waher.Runtime.Collections;

internal class Program
{
	/// <summary>
	/// Extracts certificate information from an LDIF file and saves the certificates
	/// in an output folder, ordered by country and Subject Key Identifier.
	/// 
	/// Syntax:
	/// ExtractCertificates -i INPUT_FILE -o OUTPUT_FOLDER[ -d][ -h]
	/// 
	/// Where:
	/// INPUT_FILE     is the file name of the LDIF file containing the certificates.
	/// OUTPUT_FOLDER  is the folder where the extracted certificates will be saved.
	/// 
	/// You can process multiple input files by providing multiple -i arguments, but only 
	/// one output folder with -o.
	/// 
	/// If -d is present, old files no longer present in the LDIF file will be deleted 
	/// from the output folder.
	/// 
	/// Use -h or -? to show this help message.
	/// </summary>
	/// <param name="Arguments">Command-line arguments.</param>
	private static void Main(string[] Arguments)
	{
		try
		{
			ChunkedList<string> InputFileNames = [];
			string? OutputFolder = null;
			int i = 0;
			int c = Arguments.Length;
			bool Help = false;
			bool DeleteOld = false;

			while (i < c)
			{
				switch (Arguments[i++])
				{
					case "-i":
						if (i >= c)
							throw new Exception("Expected file name.");

						InputFileNames.Add(Arguments[i++]);
						break;

					case "-o":
						if (i >= c)
							throw new Exception("Expected output folder.");

						if (string.IsNullOrEmpty(OutputFolder))
							OutputFolder = Arguments[i++];
						else
							throw new Exception("Output folder already specified.");

						break;

					case "-d":
						DeleteOld = true;
						break;

					case "-h":
					case "-?":
						Help = true;
						break;
				}
			}

			if (Help || InputFileNames.Count == 0 && string.IsNullOrEmpty(OutputFolder))
			{
				Console.Out.WriteLine("Extracts certificate information from an LDIF file and saves the certificates");
				Console.Out.WriteLine("in an output folder, ordered by country and Subject Key Identifier.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Syntax:");
				Console.Out.WriteLine("ExtractCertificates -i INPUT_FILE -o OUTPUT_FOLDER[ -d][ -h]");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Where:");
				Console.Out.WriteLine("INPUT_FILE     is the file name of the LDIF file containing the certificates.");
				Console.Out.WriteLine("OUTPUT_FOLDER  is the folder where the extracted certificates will be saved.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("You can process multiple input files by providing multiple -i arguments, but only");
				Console.Out.WriteLine("one output folder with -o.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("If -d is present, old files no longer present in the LDIF file will be deleted ");
				Console.Out.WriteLine("from the output folder.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Use -h or -? to show this help message.");

				return;
			}

			if (InputFileNames.Count == 0)
				throw new Exception("Input file name not specified.");

			if (string.IsNullOrEmpty(OutputFolder))
				throw new Exception("Output folder not specified.");

			SortedDictionary<string, int> CountsPerCountry = [];
			StringBuilder sb = new();
			Status Status = new();

			OutputFolder = Path.GetFullPath(OutputFolder);
			if (!Directory.Exists(OutputFolder))
				Directory.CreateDirectory(OutputFolder);

			foreach (string FileName in Directory.GetFiles(OutputFolder, "*.cer", SearchOption.AllDirectories))
				Status.ExistingFiles[FileName] = true;

			foreach (string FileName in InputFileNames)
			{
				string InputFileName = Path.GetFullPath(FileName);
				if (!File.Exists(InputFileName))
					throw new Exception("Input file " + InputFileName + " does not exist.");

				using FileStream f = File.OpenRead(InputFileName);
				using StreamReader r = new(f);

				string s;
				int State = 0;

				while (!r.EndOfStream)
				{
					s = r.ReadLine() ?? string.Empty;
					Status.NrRows++;

					bool Empty = string.IsNullOrEmpty(s);
					if (Empty)
						Status.NrRecords++;

					switch(State)
					{
						case 0: // Searching for certificate or master list.
							if (Empty)
								break;

							if (s.StartsWith("userCertificate;"))
							{
								s = s[16..];
								if (!s.StartsWith("binary::"))
									throw new Exception("Expected binary certificate.");

								sb.Clear();
								sb.Append(s[8..].Trim());

								State = 1;
								Status.NrCertificates++;
							}
							else if (s.StartsWith("pkdMasterListContent::"))
							{
								sb.Clear();
								sb.Append(s[22..].Trim());

								State = 2;
								Status.NrMasterLists++;
							}
							break;

						case 1: // In certificate
							if (Empty || s.Contains(':'))
							{
								CheckCertificate(sb.ToString(), Status, OutputFolder);

								sb.Clear();
								State = 0;
							}
							else
								sb.Append(s.Trim());
							break;

						case 2: // In masterlist
							if (Empty || s.Contains(':'))
							{
								CheckMasterList(sb.ToString(), Status, OutputFolder);

								sb.Clear();
								State = 0;
							}
							else
								sb.Append(s.Trim());
							break;
					}
				}

				switch (State)
				{
					case 1: // In certificate
						Status.NrCertificates++;
						CheckCertificate(sb.ToString(), Status, OutputFolder);
						break;

					case 2: // In masterlist
						Status.NrMasterLists++;
						CheckMasterList(sb.ToString(), Status, OutputFolder);
						break;
				}
			}

			Console.Out.WriteLine();
			Console.Out.WriteLine("Nr Rows: " + Status.NrRows.ToString());
			Console.Out.WriteLine("Nr Records: " + Status.NrRecords.ToString() + " (including version)");
			Console.Out.WriteLine("Nr Certificates: " + Status.NrCertificates.ToString());
			Console.Out.WriteLine("Nr Master Lists: " + Status.NrMasterLists.ToString());
			Console.Out.WriteLine("Nr Errors: " + Status.NrErrors.ToString());
			Console.Out.WriteLine("Nr No Country CA: " + Status.NrNoCountry.ToString());
			Console.Out.WriteLine("Nr No Subject Key ID: " + Status.NrNoSubjectKeyId.ToString());
			Console.Out.WriteLine("Nr Subject Key duplicates: " + Status.NrSubjectKeyIdDuplicates.ToString());
			Console.Out.WriteLine("Nr New Folders: " + Status.NrNewFolders.ToString());
			Console.Out.WriteLine("Nr New Files: " + Status.NrNewCertificates.ToString());
			Console.Out.WriteLine("Nr Old Files: " + Status.ExistingFiles.Count.ToString());

			if (DeleteOld && Status.ExistingFiles.Count > 0)
			{
				int NrDeleted = 0;
				int NrNotDeleted = 0;

				foreach (string FileName in Status.ExistingFiles.Keys)
				{
					try
					{
						File.Delete(FileName);
						NrDeleted++;
					}
					catch (Exception)
					{
						NrNotDeleted++;
					}
				}

				Console.Out.WriteLine("Nr old files deleted: " + NrDeleted.ToString());
				Console.Out.WriteLine("Nr old files not deleted: " + NrNotDeleted.ToString());
			}

			foreach (KeyValuePair<string, int> P in CountsPerCountry)
				Console.Out.WriteLine(P.Key + ": " + P.Value.ToString());
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}

	private class Status
	{
		public Dictionary<string, bool> ExistingFiles = [];
		public Dictionary<string, bool> FilesProcessed = [];
		public SortedDictionary<string, int> CountsPerCountry = [];
		public int NrRows = 0;
		public int NrRecords = 0;
		public int NrCertificates = 0;
		public int NrMasterLists = 0;
		public int NrErrors = 0;
		public int NrNoCountry = 0;
		public int NrNoSubjectKeyId = 0;
		public int NrSubjectKeyIdDuplicates = 0;
		public int NrNewCertificates = 0;
		public int NrNewFolders = 0;
	}

	private static void CheckCertificate(string Base64, Status Status, string OutputFolder)
	{
		byte[] Bin = Convert.FromBase64String(Base64);

		try
		{
			X509Certificate2 Certificate = new(Bin);
			CheckCertificate(Certificate, Bin, Status, OutputFolder);
		}
		catch (Exception)
		{
			Status.NrErrors++;
		}
	}

	private static void CheckCertificate(X509Certificate2 Certificate, byte[] Bin,
		Status Status, string OutputFolder)
	{
		string Subject = Certificate.SubjectName.Name;
		string Country;
		int i;
		string? SubjectKeyId = null;

		i = Subject.IndexOf("C=");
		if (i < 0)
		{
			Status.NrNoCountry++;
			return;
		}
		else
		{
			Country = Subject[(i + 2)..];
			i = Country.IndexOfAny([',', ' ']);

			if (i >= 0)
				Country = Country[..i];

			Country = Country.ToUpper();
		}

		if (Status.CountsPerCountry.TryGetValue(Country, out i))
			Status.CountsPerCountry[Country] = i + 1;
		else
			Status.CountsPerCountry[Country] = 1;

		foreach (X509Extension E in Certificate.Extensions)
		{
			if (E is X509SubjectKeyIdentifierExtension SubjectKeyIdentifier)
			{
				SubjectKeyId = SubjectKeyIdentifier.SubjectKeyIdentifier;
				break;
			}
		}

		if (string.IsNullOrEmpty(SubjectKeyId))
		{
			Status.NrNoSubjectKeyId++;
			return;
		}

		OutputFolder = Path.Combine(OutputFolder, Country);
		if (!Directory.Exists(OutputFolder))
		{
			Directory.CreateDirectory(OutputFolder);
			Status.NrNewFolders++;
		}

		string FileName = Path.Combine(OutputFolder, SubjectKeyId + ".cer");

		Status.ExistingFiles.Remove(FileName);

		if (Status.FilesProcessed.ContainsKey(FileName))
			Status.NrSubjectKeyIdDuplicates++;
		else
			Status.FilesProcessed[FileName] = true;

		if (!File.Exists(FileName))
		{
			Status.NrNewCertificates++;
			File.WriteAllBytes(FileName, Bin);
		}
	}

	private static void CheckMasterList(string Base64, Status Status, string OutputFolder)
	{
		byte[] Bin = Convert.FromBase64String(Base64);
		bool ProcessingCert = false;

		try
		{
			SignedCms SignedData = new();
			SignedData.Decode(Bin);

			foreach (X509Certificate2 Certificate in SignedData.Certificates)
				CheckCertificate(Certificate, Certificate.RawData, Status, OutputFolder);

			if (!TryDecodeDER(SignedData.ContentInfo.Content, out object? Content))
			{
				Status.NrErrors++;
				return;
			}

			ProcessingCert = true;

			if (Content is Vector ContentVector)
			{
				foreach (object? Element in ContentVector.Elements)
				{
					if (Element is Vector VectorElement)
					{
						foreach (object? Element2 in VectorElement.Elements)
						{
							if (Element2 is Vector VectorElement2)
							{
								Bin = VectorElement2.Binary;
								X509Certificate2 Certificate = new(Bin);
								CheckCertificate(Certificate, Bin, Status, OutputFolder);
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Console.Out.WriteLine("Error processing master list: " + ex.Message);

			if (ProcessingCert)
			{
				Console.Out.WriteLine("Certificate binary provoking error:");
				Console.Out.WriteLine(Convert.ToBase64String(Bin));
			}

			Status.NrErrors++;
		}
	}

	/// <summary>
	/// Decodes a DER-encoded object.
	/// </summary>
	/// <param name="Data">Binary data</param>
	/// <param name="Value">Decoded object.</param>
	/// <returns>If successful.</returns>
	public static bool TryDecodeDER(byte[] Data, out object? Value)
	{
		AsnReader Reader = new(Data, AsnEncodingRules.DER);
		return TryDecodeDERNext(Reader, out Value);
	}

	private static bool TryDecodeDERNext(AsnReader Reader, out object? Value)
	{
		if (!Reader.HasData)
		{
			Value = null;
			return false;
		}

		Asn1Tag Tag = Reader.PeekTag();

		if (Tag.TagClass == TagClass.Universal)
		{
			switch (Tag.TagValue)
			{
				case (int)UniversalTagNumber.EndOfContents:
					Value = null;
					return false;

				case (int)UniversalTagNumber.Boolean:
					Value = Reader.ReadBoolean();
					return true;

				case (int)UniversalTagNumber.Integer:
				case (int)UniversalTagNumber.Enumerated:
					Value = Reader.ReadInteger();
					return true;

				case (int)UniversalTagNumber.BitString:
					byte[] Bin = Reader.ReadBitString(out int BitCount);
					BitArray Bits = new(Bin)
					{
						Length = BitCount
					};
					Value = Bits;
					return true;

				case (int)UniversalTagNumber.OctetString:
					Bin = Reader.ReadOctetString();

					try
					{
						if (TryDecodeDER(Bin, out object? Embedded))
							Value = Embedded;
						else
							Value = Bin;
					}
					catch (Exception)
					{
						Value = Bin;
					}

					return true;

				case (int)UniversalTagNumber.Null:
					Reader.ReadNull();
					Value = null;
					return true;

				case (int)UniversalTagNumber.ObjectIdentifier:
					Value = Reader.ReadObjectIdentifier();
					return true;

				case (int)UniversalTagNumber.ObjectDescriptor:  // Obsolete
				case (int)UniversalTagNumber.UTF8String:
				case (int)UniversalTagNumber.NumericString:
				case (int)UniversalTagNumber.PrintableString:
				case (int)UniversalTagNumber.TeletexString:     // Same as UniversalTagNumber.T61String:
				case (int)UniversalTagNumber.VideotexString:
				case (int)UniversalTagNumber.IA5String:
				case (int)UniversalTagNumber.GraphicString:
				case (int)UniversalTagNumber.VisibleString:     // Same as UniversalTagNumber.ISO646String:
				case (int)UniversalTagNumber.GeneralString:
				case (int)UniversalTagNumber.UniversalString:
				case (int)UniversalTagNumber.UnrestrictedCharacterString:
				case (int)UniversalTagNumber.BMPString:
					Value = Reader.ReadCharacterString((UniversalTagNumber)Tag.TagValue);
					return true;

				case (int)UniversalTagNumber.Real:
				case (int)UniversalTagNumber.RelativeObjectIdentifier:
				case (int)UniversalTagNumber.Time:
				case (int)UniversalTagNumber.Date:
				case (int)UniversalTagNumber.TimeOfDay:
				case (int)UniversalTagNumber.DateTime:
				case (int)UniversalTagNumber.Duration:
				case (int)UniversalTagNumber.ObjectIdentifierIRI:
				case (int)UniversalTagNumber.RelativeObjectIdentifierIRI:
					Value = Reader.ReadEncodedValue();
					return true;

				case (int)UniversalTagNumber.Sequence:          // Same as UniversalTagNumber.SequenceOf:
				case (int)UniversalTagNumber.External:          // Same as UniversalTagNumber.InstanceOf:
				case (int)UniversalTagNumber.Set:               // Same as UniversalTagNumber.SetOf:
				case (int)UniversalTagNumber.Embedded:

					ReadOnlyMemory<byte> Section = Reader.ReadEncodedValue();
					AsnReader Inner = new(Section, Reader.RuleSet);

					if (Tag.TagValue == (int)UniversalTagNumber.Sequence)
						Inner = Inner.ReadSequence();
					else
						Inner = Inner.ReadSetOf(Tag);

					if (!TryDecodeDERNext(Inner, out object? FirstElement))
					{
						Value = Array.Empty<object?>();
						return true;
					}

					if (!TryDecodeDERNext(Inner, out object? Element))
					{
						Value = new Vector([FirstElement], Section.ToArray());
						return true;
					}

					ChunkedList<object?> Elements = [FirstElement, Element];

					while (TryDecodeDERNext(Inner, out Element))
						Elements.Add(Element);

					Value = new Vector([.. Elements], Section.ToArray());
					return true;

				case (int)UniversalTagNumber.UtcTime:
					Value = Reader.ReadUtcTime();
					return true;

				case (int)UniversalTagNumber.GeneralizedTime:
					Value = Reader.ReadGeneralizedTime();
					return true;

				default:
					Value = null;
					return false;
			}
		}
		else if (Tag.TagClass == TagClass.ContextSpecific)
		{
			ReadOnlyMemory<byte> Section = Reader.ReadEncodedValue();

			if (Tag.IsConstructed)
			{
				AsnReader Inner = new(Section, Reader.RuleSet);

				try
				{
					Inner = Inner.ReadSequence(Tag);

					if (!TryDecodeDERNext(Inner, out object? FirstElement))
					{
						Value = Array.Empty<object?>();
						return true;
					}

					if (!TryDecodeDERNext(Inner, out object? Element))
					{
						Value = new Vector([FirstElement], Section.ToArray());
						return true;
					}

					ChunkedList<object?> Elements = [FirstElement, Element];

					while (TryDecodeDERNext(Inner, out Element))
						Elements.Add(Element);

					Value = new Vector([.. Elements], Section.ToArray());
					return true;
				}
				catch (Exception)
				{
					Value = Section.ToArray();
					return true;
				}
			}
			else
			{
				Value = Section.ToArray();
				return true;
			}
		}
		else
		{
			Value = null;
			return false;
		}
	}

	private class Vector
	{
		public object?[] Elements;
		public byte[] Binary;

		public Vector(object?[] Elements, byte[] Binary)
		{
			this.Elements = Elements;
			this.Binary = Binary;
		}
	}

}