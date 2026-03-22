ICAO Public-Key-Infrastructure Certificates
==============================================

[ICAO publishes](https://pkddownloadsg.icao.int/) a list of public certificates used by
certificate authorities in issuing certificates for electronic travel documents. When
validating such certificates, the application needs to access the published certificates from 
ICAO, as the normal certificate validation mechanism will not work otherwise.

This repository contains a copy of the certificates published by ICAO, and a utility 
application called [`ExtractCertificates`](ExtractCertificates) that can extract the 
certificates from the LDIF files published by ICAO. Certificates are ordered by country and 
Subject Key Identifier (SKI). Certificates available in Travel documents reference these 
issuer certificates using a corresponding Auhority Key Identifier (AKI) that matches the 
SKI of the issuer certificate.

Certificate files are stored in the package folder `Root/IcaoPki`. The 
[`IcaoPkiCertificates.manifest`](IcaoPkiCertificates.manifest) file contains a list of all
certificates, and can be used to create a distributable package for the TAG Neuron.

### ExtractCertificates

The `ExtractCertificates` utility application can be used to extract certificates from 
ICAO LDIF files. It takes as input the path to the LDIF file, and outputs the extracted
certificates in CER format. The LDIF file can encode certificates directly, or include
signed master files containing sets of certificates. The application can be run from the 
command line, and supports the following arguments:

```
ExtractCertificates -i INPUT_FILE -o OUTPUT_FOLDER[ -d][ -h]
```

Where:

- `INPUT_FILE` is the path to the LDIF file containing the certificates.
- `OUTPUT_FOLDER` is the path to the folder where the extracted certificates will be saved.

You can process multiple input files by providing multiple -i arguments, but only 
one output folder with -o.

If `-d` is present, old files no longer present in the LDIF file will be deleted from the 
output folder.

Use `-h` or `-?` to show the Command-Line help message.

The [IoTGateway](https://github.com/PeterWaher/IoTGateway) repository contains a utility
application called [`GenManifest`](https://github.com/PeterWaher/IoTGateway/tree/master/Utilities/Waher.Utility.GenManifest)
that is used to generate the manifest file for the extracted certificates. The
[`Install`](https://github.com/PeterWaher/IoTGateway/tree/master/Utilities/Waher.Utility.Install)
utility is then used to create a distributable package for the TAG Neuron, and the
[`Sign`](https://github.com/PeterWaher/IoTGateway/tree/master/Utilities/Waher.Utility.Sign)
utility is used to sign the package for distribution.

### Public ICAO certificates

The [`Root/IcaoPki`](Root/IcaoPki) folder contains the public certificates published by ICAO, 
ordered by country and SKI. Each certificate is stored in a separate CER file, named according 
to the country and SKI of the certificate. The certificates are in DER format, and can be used 
for validation of electronic travel documents that reference them using their AKI.

### Updating list of certificates

ICAO regularly updates the list of public certificates, and new certificates may be added or 
old certificates may be removed. To update the list of certificates, you first need to
[downloadt hem from ICAO](https://pkddownloadsg.icao.int/). To get a complete list, you need
to download the following two files:

* `icaopkd-001-complete-009875.ldif`, containg eMRTD Certificates (DSC, BCSC, BCSC-NC) and CRL.
* `icaopkd-002-complete-000340.ldif`, containing the CSCA MasterList.

You can then execute the [`UpdateFiles.bat` batch file](UpdateFiles.bat) in the root of this 
repository, having built the corresponding utilities necessary. The batch file will update the 
certificate folers, and the corresponding manifest file for the packege. The batch file 
assumes the downloaded files reside in the parent folder of the solution folder.
