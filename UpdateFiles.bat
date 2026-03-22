ExtractCertificates\bin\Debug\net8.0\ExtractCertificates.exe ^
	-i ..\icaopkd-002-complete-000340.ldif ^
	-i ..\icaopkd-001-complete-009875.ldif ^
	-o Root\IcaoPki ^
	-d

..\IoTGateway\Utilities\Waher.Utility.GenManifest\bin\Debug\net8.0\Waher.Utility.GenManifest.exe ^
	-c "." ^
	-ef .git ^
	-ef .vs ^
	-ef ExtractCertificates ^
	-ex .gitattributes ^
	-ex .gitignore ^
	-ex .bat ^
	-ex .manifest ^
	-ex .sln ^
	-ex .md ^
	-o IcaoPkiCertificates.manifest
