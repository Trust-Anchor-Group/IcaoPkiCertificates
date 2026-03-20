ICAO Public-Key-Infrastructure Certificates
==============================================

[ICAO publishes](https://pkddownloadsg.icao.int/) a list of public certificates used by
certificate authorities in issuing certificates for electronic travel documents. When
validating such certificates, the application needs to access the published certificates from 
ICAO, as the normal certificate validation mechanism will not work otherwise.

This repository contains a copy of the certificates published by ICAO, and a utility 
application that can extract the certificates from the LDIF files published by ICAO.
Certificates are ordered by country and Subject Key Identifier (SKI). Certificates available
in Travel documents reference these issuer certificates using a corresponding
Auhority Key Identifier (AKI) that matches the SKI of the issuer certificate.

