# BlocklistManager

## Applicaation objective
To enhance computer security using IP address blocklists to generate Windows Firewall rules.
This application cannot guarantee protection against all external threats. It blocks inbound traffic to, and outbound traffic from, your computer by creating firewall rules for all IP (V4 and V6) addresses found in more than 20 public IP address blocklists.

Buy me a beer (USD8) if you feel that this app makes youur life safer and easier!

## .NET versions
This project was developed using .NET 8.0 and C# 12.0 and has also been built and tested using .NET 9.0. Older .NET versions are not supported.

## Operating systems and user interface
This app currently only manages rules in Windows Defender Firewall and is therefore **not portable to other operating systems**. 
I would like to change it to manage blocklist rules at network firewall level but that would be a far more complex exercise so is not imminent.
This version has a WinForms interface to help you to get started. It's neither the slickest or most modern interface but then, it's not intended for frequent use.

## Dependencies
The project has the following dependencies:

### Microsoft frameworks
- Microsoft.NETCore.App
- Microsoft.Windows.SDK.NEt.Ref.Windows
- Microsoft.WindowsDesktop.App.WindowsForms

### Nuget packages
- Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Relational and Microsoft.EntityFrameworkCore.SqlServer, all versions 9.0.1
- Microsoft.Extensions.Configuration.Json version 9.0.1
- SharpCompress version 0.39.0 [https://github.com/adamhathcock/sharpcompress](https://github.com/adamhathcock/sharpcompress)
- TaskScheduler version 2.11.0 [https://github.com/dahall/TaskScheduler](https://github.com/dahall/TaskScheduler)
- WindowsFirewallHelper version 2.2.0.86 [https://github.com/falahati/WindowsFirewallHelperv](https://github.com/falahati/WindowsFirewallHelper)

### Other
- OSVersionExtension, recompiled with .NET 8.0 [https://github.com/pruggitorg/detect-windows-version](https://github.com/pruggitorg/detect-windows-version), not the same version as available iNuget version.
- SBS.Utilities.dll, version

I recommend that this be scheduled to run outside your working hours as it still uses what I regard as excessive resources... but then, it's working to add firewall rules for hundreds of thousands of IP addresses, IP address ranges and subnets.
I have tried to minimise the memory use of app by only using active dependencies, reducing the size of each preliminary rule etc.
I've also tried different ways (netsh, powershell etc.) of working with the Windows 11 firewall. All take time to add or remove rules. Remember that even after deduplicating addresses and consolidating them into batches of 1000, there are over 1,000 firewall rules (inbound and outbound).


