# Licenses and Copyrights

The file `LICENSE.TXT` in the main folder of the repo is the leading license
information, even if it does not show up in the Visual Studio solution. To
update all dependent license files, manually start `src\CopyLicense.ps1`.

Some plug-ins require a separate license. These separate licenses are given
in `LICENSE-PLUGIN.txt` in the corresponding directory of the plug-in. 

You should add a copyright note for each major contribution in the corresponding
files:
1) The copyright note should start with `Copyright (c)` followed by the time 
   span of the contribution.
    
2) You should put the full name of the legal entity (*e.g.*, 
   `Phoenix Contact GmbH & Co. KG`); a simple name (Phoenix Contact) will *not* 
   do! 
3) Each copyright note should indicate the contact to the legal entity in the 
   angle brackets (*e.g.*, 
  `<https://www.festo.com/net/de_de/Forms/web/contact_international>`).
4) The author of the contribution is optional (unless the contributor is a 
   private person, of course). If there are multiple authors, please indicate 
   only the organization and omit the individual contributors.
5) All the copyrights of a file are joined into a single comment block 
   (`/* ... */`).
6) Reference all the used libraries and dependencies in the copyright block as
   well.

Example copyright block in the source code:
```cs
/*
Copyright (c) 2018-2019 Festo AG & Co. KG 
<https://www.festo.com/net/de_de/Forms/web/contact_international>             
Author: Michael Hoffmeister

Copyright (c) 2019-2020 PHOENIX CONTACT GmbH & Co. KG 
<opensource@phoenixcontact.com> 
Author: Andreas Orzelski

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license 
(see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v2.0 
(see http://www.apache.org/licenses/LICENSE-2.0).
*/
``` 

Please make sure to add the copyright notes in the main [`LICENSE.txt`](
https://github.com/admin-shell-io/aasx-package-explorer/blob/master/LICENSE.txt
) file as well and call `src\CopyLicense.ps1` on changes.
The licenses of the dependencies have to be listed in the main LICENSE.txt as
well.

Example of the LICENSE.txt header:

```
Copyright (c) 2018-2020 Festo AG & Co. KG 
<https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2020 PHOENIX CONTACT GmbH & Co. KG 
<opensource@phoenixcontact.com>
Author: Andreas Orzelski

Copyright (c) 2019-2020 Fraunhofer IOSB-INA Lemgo, 
    eine rechtlich nicht selbstaendige Einrichtung der Fraunhofer-Gesellschaft
    zur Foerderung der angewandten Forschung e.V.

Copyright (c) 2020 Schneider Electric Automation GmbH 
<marco.mendes@se.com>
Author: Marco Mendes

... more contributors ...

This software is licensed under the Apache License 2.0 (Apache-2.0) (see below)

The browser functionality is licensed under the cefSharp license (see below)
The QR code generation is licensed under the MIT license (MIT) (see below)

... more references to dependencies ...
```

Example of the dependency license down in the body of LICENSE.txt:
```
 With respect to cefSharp
=========================
(https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE)
// Copyright © The CefSharp Authors. All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
...
```

## Contributor License Agreement (CLA)

Every contributor needs to sign the Contributor License Agreement (CLA). 
Thanks to [CLA-assistant][cla-assistant] GitHub plug-in, this can be done 
on-line. You can see the CLA here:
https://cla-assistant.io/admin-shell-io/aasx-package-explorer

When you create a pull request, you will be automatically asked to sign the CLA.
Simply click on the button, click that you agree to CLA and the agreement should
be signed.

[cla-assistant]: https://cla-assistant.io/
