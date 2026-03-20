internal class Program
{
	/// <summary>
	/// Extracts certificate information from an LDIF file and saves the certificates
	/// in an output folder, ordered by country and Subject Key Identifier.
	/// 
	/// Syntax:
	/// ExtractCertificates -i INPUT_FILE -o OUTPUT_FOLDER
	/// 
	/// Where:
	/// INPUT_FILE     is the file name of the LDIF file containing the certificates.
	/// OUTPUT_FOLDER  is the folder where the extracted certificates will be saved.
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
				Console.Out.WriteLine("ExtractCertificates -i INPUT_FILE -o OUTPUT_FOLDER");
				Console.Out.WriteLine();
				Console.Out.WriteLine("Where:");
				Console.Out.WriteLine("INPUT_FILE     is the file name of the LDIF file containing the certificates.");
				Console.Out.WriteLine("OUTPUT_FOLDER  is the folder where the extracted certificates will be saved.");

				return;
			}

			if (string.IsNullOrEmpty(InputFileName))
				throw new Exception("Input file name not specified.");

			if (string.IsNullOrEmpty(OutputFolder))
				throw new Exception("Output folder not specified.");

			InputFileName = Path.GetFullPath(InputFileName);
			if (!File.Exists(InputFileName))
				throw new Exception("Input file " + InputFileName + " does not exist.");

			OutputFolder = Path.GetFullPath(OutputFolder);
			if (!Directory.Exists(OutputFolder))
				Directory.CreateDirectory(OutputFolder);


		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}
}