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
certificates in CER format. The application can be run from the command line, and supports 
the following arguments:

```
ExtractCertificates -i INPUT_FILE -o OUTPUT_FOLDER[ -d][ -h]
```

Where:

- `INPUT_FILE` is the path to the LDIF file containing the certificates.
- `OUTPUT_FOLDER` is the path to the folder where the extracted certificates will be saved.

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