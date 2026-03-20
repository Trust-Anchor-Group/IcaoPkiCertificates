using System.Security.Cryptography.X509Certificates;
using System.Text;

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
			string? InputFileName = null;
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

						if (string.IsNullOrEmpty(InputFileName))
							InputFileName = Arguments[i++];
						else
							throw new Exception("Input file name already specified.");

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

			if (Help || (string.IsNullOrEmpty(InputFileName) && string.IsNullOrEmpty(OutputFolder)))
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
				Console.Out.WriteLine("If -d is present, old files no longer present in the LDIF file will be deleted ");
				Console.Out.WriteLine("from the output folder.");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Use -h or -? to show this help message.");

				return;
			}

			if (string.IsNullOrEmpty(InputFileName))
				throw new Exception("Input file name not specified.");

			if (string.IsNullOrEmpty(OutputFolder))
				throw new Exception("Output folder not specified.");

			InputFileName = Path.GetFullPath(InputFileName);
			if (!File.Exists(InputFileName))
				throw new Exception("Input file " + InputFileName + " does not exist.");

			using FileStream f = File.OpenRead(InputFileName);
			using StreamReader r = new(f);

			OutputFolder = Path.GetFullPath(OutputFolder);
			if (!Directory.Exists(OutputFolder))
				Directory.CreateDirectory(OutputFolder);
			
			SortedDictionary<string, int> CountsPerCountry = [];
			StringBuilder sb = new();
			Status Status = new();
			string s;
			bool InCertificate = false;

			foreach (string FileName in Directory.GetFiles(OutputFolder, "*.cer", SearchOption.AllDirectories))
				Status.ExistingFiles[FileName] = true;

			while (!r.EndOfStream)
			{
				s = r.ReadLine() ?? string.Empty;
				Status.NrRows++;

				bool Empty = string.IsNullOrEmpty(s);
				if (Empty)
					Status.NrRecords++;

				if (InCertificate)
				{
					if (Empty || s.Contains(':'))
					{
						CheckCertificate(sb.ToString(), Status, OutputFolder);

						sb.Clear();
						InCertificate = false;
					}
					else
						sb.Append(s.Trim());
				}
				else if (!Empty && s.StartsWith("userCertificate;"))
				{
					s = s[16..];
					if (!s.StartsWith("binary::"))
						throw new Exception("Expected binary certificate.");

					sb.Clear();
					sb.Append(s[8..].Trim());

					InCertificate = true;
					Status.NrCertificates++;
				}
			}

			if (InCertificate)
			{
				Status.NrCertificates++;
				CheckCertificate(sb.ToString(), Status, OutputFolder);
			}

			Console.Out.WriteLine();
			Console.Out.WriteLine("Nr Rows: " + Status.NrRows.ToString());
			Console.Out.WriteLine("Nr Records: " + Status.NrRecords.ToString() + " (including version)");
			Console.Out.WriteLine("Nr Certificates: " + Status.NrCertificates.ToString());
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
			X509Certificate2 Cert = new(Bin);
			string Subject = Cert.SubjectName.Name;
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

			foreach (X509Extension E in Cert.Extensions)
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
		catch (Exception)
		{
			Status.NrErrors++;
		}
	}
}