ExtractCertificates\bin\Debug\net8.0\ExtractCertificates.exe ^
	-i ..\icaopkd-002-complete-530.ldif ^
	-i ..\icaopkd-001-complete-10332.ldif ^
	-i ..\icaopkd-003-complete-10.ldif ^
	-i ..\icaopkd-004-complete-10.ldif ^
	-i ..\icaopkd-005-complete-11.ldif ^
	-f Manual ^
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
