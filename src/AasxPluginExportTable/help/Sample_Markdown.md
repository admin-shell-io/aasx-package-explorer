# 3 Heading

## 3.1 Nameplate


| idShort:| Nameplate| | |
| --- | --- | --- | --- |
| Class:| Submodel| | |
| semanticId:| [IRI] https://admin-shell.io/zvei/nameplate/1/0/Nameplate| | |
| Parent:| Nameplate| | |
| Explanation:| -| | |

| [SME type]<br/>idShort| semanticId = [idType]value<br/>Description@en| [valueType]<br/>example| card.|
| --- | --- | --- | --- |
| [MLP]<br/>ManufacturerName| [IRDI]0173-1#02-AAO677#002<br/>Manufacturer name legally valid designation of the natural or judicial person which is directly responsible for the design, production, packaging and labeling of a product in respect to its being brought into circulation -| [-]<br/>FESTO@de, FESTO@en| |
| [MLP]<br/>ManufacturerProductDesignation| [IRDI]0173-1#02-AAW338#001<br/>Manufacturer product designation Short description of the product (short text) -| [-]<br/>Einzigartig flexibel im Anschluss: der Drucksensor SPAU. Ob Druckmessung, Drucküberwachung oder Druckabfrage – Sie haben alle Druckwerte immer auf einen Blick unter Kontrolle. Im IO-Link-Mode sind Fernwartung und -parametrierung sowie eine einfache Sensorenreplizierung möglich.@de, The pressure sensor SPAU has a uniquely flexible connection. Whether for pressure measuring, pressure monitoring or pressure sensing, all pressure values are always under control at a glance. Remote maintenance and parameterisation as well as simple sensor replication are possible in IO-Link® mode.@en| |
| [MLP]<br/>ManufacturerProductFamily| [IRDI]0173-1#02-AAU731#001<br/>Manufacturer product family 2nd level of a 3 level manufacturer specific product hierarchy -| [-]<br/>SPAU@de, SPAU@en| |
| [Property]<br/>SerialNumber| [IRDI]0173-1#02-AAM556#002<br/>serial number unique combination of numbers and letters used to identify the device once it has been manufactured -| [string]<br/>| |
| [Property]<br/>YearOfConstruction| [IRDI]0173-1#02-AAP906#001<br/>Year of construction Year as completion date of object -| [string]<br/>| |
| [SMC]<br/>Address| [IRDI]0173-1#02-AAQ832#005<br/>Address Address information of a business partner -| [-]<br/>12 elements| 1..*|
| [SMC]<br/>Markings| [IRI]https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings<br/>Markings Collection of product markings -| [-]<br/>4 elements| |


## 3.2 Address


| idShort:| Address| | |
| --- | --- | --- | --- |
| Class:| SubmodelElementCollection| | |
| semanticId:| [IRDI] 0173-1#02-AAQ832#005| | |
| Parent:| Address| | |
| Explanation:| -| | |

| [SME type]<br/>idShort| semanticId = [idType]value<br/>Description@en| [valueType]<br/>example| card.|
| --- | --- | --- | --- |
| [MLP]<br/>Department| [IRDI]0173-1#02-AAO127#003<br/>department administrative section within an organization where a business partner is located -| [-]<br/>Kontakt zu Festo@de, Contact to Festo@en| |
| [MLP]<br/>Street| [IRDI]0173-1#02-AAO128#002<br/>street street name and house number -| [-]<br/>Ruiter Straße 82@de, Ruiter Straße 82@en| |
| [MLP]<br/>ZipCode| [IRDI]0173-1#02-AAO129#002<br/>zip code ZIP code of address -| [-]<br/>73734@de, 73734@en| |
| [MLP]<br/>POBox| [IRDI]0173-1#02-AAO130#002<br/>PO box P.O. box number -| [-]<br/>| |
| [MLP]<br/>ZipCodeOfPOBox| [IRDI]0173-1#02-AAO131#002<br/>zip code of PO box ZIP code of P.O. box address -| [-]<br/>| |
| [MLP]<br/>City_Town| [IRDI]0173-1#02-AAO132#002<br/>city/town town or city -| [-]<br/>| |
| [MLP]<br/>State_County| [IRDI]0173-1#02-AAO133#002<br/>state/county federal state a part of a state -| [-]<br/>Baden-Wuerttemberg@de, Baden-Württemberg@en| |
| [MLP]<br/>NationalCode| [IRDI]0173-1#02-AAO134#002<br/>national code code of a country -| [-]<br/>| |
| [MLP]<br/>VATNumber| [IRDI]0173-1#02-AAO135#002<br/>VAT number VAT identification number of the business partner -| [-]<br/>DE 145 339 206@de, DE 145 339 206@en| |
| [MLP]<br/>AddressRemarks| [IRDI]0173-1#02-AAO202#003<br/>address remarks plain text characterizing address information for which there is no property -| [-]<br/>| |
| [Property]<br/>AddressOfAdditionalLink| [IRDI]0173-1#02-AAQ326#002<br/>address of additional link web site address where information about the product or contact is given -| [string]<br/>www.festo.com/contact| |
| [SMC]<br/>Phone| [IRDI]0173-1#02-AAQ833#005<br/>Phone Phone number including type -| [-]<br/>2 elements| |


## 3.3 Phone


| idShort:| Phone| | |
| --- | --- | --- | --- |
| Class:| SubmodelElementCollection| | |
| semanticId:| [IRDI] 0173-1#02-AAQ833#005| | |
| Parent:| Phone| | |
| Explanation:| -| | |

| [SME type]<br/>idShort| semanticId = [idType]value<br/>Description@en| [valueType]<br/>example| card.|
| --- | --- | --- | --- |
| [MLP]<br/>TelephoneNumber| [IRDI]0173-1#02-AAO136#002<br/>telephone number complete telephone number to be called to reach a business partner -| [-]<br/>+49 (0) 711 347 0@de, +49 (0) 711 347 0@en| |
| [Property]<br/>TypeOfTelephone| [IRDI]0173-1#02-AAO137#003<br/>type of telephone characterization of a telephone according to its location or usage -| [string]<br/>| |


## 3.4 Markings


| idShort:| Markings| | |
| --- | --- | --- | --- |
| Class:| SubmodelElementCollection| | |
| semanticId:| [IRI] https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings| | |
| Parent:| Markings| | |
| Explanation:| -| | |

| [SME type]<br/>idShort| semanticId = [idType]value<br/>Description@en| [valueType]<br/>example| card.|
| --- | --- | --- | --- |
| [SMC]<br/>Marking00| [IRI]https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking<br/>Marking contains information about the marking labelled on the device -| [-]<br/>2 elements| |
| [SMC]<br/>Marking01| [IRI]https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking<br/>Marking contains information about the marking labelled on the device -| [-]<br/>2 elements| |
| [SMC]<br/>Marking02| [IRI]https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking<br/>Marking contains information about the marking labelled on the device -| [-]<br/>1 elements| |
| [SMC]<br/>Marking03| [IRI]https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking<br/>Marking contains information about the marking labelled on the device -| [-]<br/>1 elements| |


## 3.5 Marking00


| idShort:| Marking00| | |
| --- | --- | --- | --- |
| Class:| SubmodelElementCollection| | |
| semanticId:| [IRI] https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking| | |
| Parent:| Marking00| | |
| Explanation:| -| | |

| [SME type]<br/>idShort| semanticId = [idType]value<br/>Description@en| [valueType]<br/>example| card.|
| --- | --- | --- | --- |
| [Property]<br/>MarkingName| [IRI]https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking/MarkingName<br/>Marking name common name of the marking -| [string]<br/>nach EU-EMV-Richtlinie| |
| [File]<br/>MarkingFile| [IRI]https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking/MarkingFile<br/>marking file picture of the marking -| [-]<br/>/aasx/Nameplate/CE_Marking_2016.png| |


## 3.6 Marking01


| idShort:| Marking01| | |
| --- | --- | --- | --- |
| Class:| SubmodelElementCollection| | |
| semanticId:| [IRI] https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking| | |
| Parent:| Marking01| | |
| Explanation:| -| | |

| [SME type]<br/>idShort| semanticId = [idType]value<br/>Description@en| [valueType]<br/>example| card.|
| --- | --- | --- | --- |
| [Property]<br/>MarkingName| [IRI]https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking/MarkingName<br/>Marking name common name of the marking -| [string]<br/>nach EU-RoHS-Richtlinie| |
| [File]<br/>MarkingFile| [IRI]https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking/MarkingFile<br/>marking file picture of the marking -| [-]<br/>/aasx/Nameplate/CE_Marking_2016.png| |


## 3.7 Marking02


| idShort:| Marking02| | |
| --- | --- | --- | --- |
| Class:| SubmodelElementCollection| | |
| semanticId:| [IRI] https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking| | |
| Parent:| Marking02| | |
| Explanation:| -| | |

| [SME type]<br/>idShort| semanticId = [idType]value<br/>Description@en| [valueType]<br/>example| card.|
| --- | --- | --- | --- |
| [Property]<br/>MarkingName| [IRI]https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking/MarkingName<br/>Marking name common name of the marking -| [string]<br/>RCM Mark| |


## 3.8 Marking03


| idShort:| Marking03| | |
| --- | --- | --- | --- |
| Class:| SubmodelElementCollection| | |
| semanticId:| [IRI] https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking| | |
| Parent:| Marking03| | |
| Explanation:| -| | |

| [SME type]<br/>idShort| semanticId = [idType]value<br/>Description@en| [valueType]<br/>example| card.|
| --- | --- | --- | --- |
| [Property]<br/>MarkingName| [IRI]https://admin-shell.io/zvei/nameplate/1/0/Nameplate/Markings/Marking/MarkingName<br/>Marking name common name of the marking -| [string]<br/>c UL us - Listed (OL)| |


