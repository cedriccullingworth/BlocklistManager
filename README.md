# BlocklistManager

This Microsoft Windows application manages Windows Firewall rules for IP addresses found in IP address blocklists. It can be run interactively or be scheduled to run at regular intervals. The user can select the blocklists to be processed by changing the command line parameters.

- Interactive mode: Run the program without any command line parameters to open the Windows Forms interface.
- Command line mode: Run the program with the following command line parameters:
  - `/sites:` followed by a semicolon-separated list of blocklist IDs to specify the blocklists to be processed, e.g. '/sites:1;2;3'. Enter the value 'allCurrent' to process all active blocklists (e.g. '/sites:allCurrent').
  - `/logpath:` followed by the path to the log file (e.g. '/logpath:C:\Logs').

**NOTE: The application needs to be run with administrator privileges to be able to change Windows Firewall rules.**

## Application objective
To enhance computer security using IP address blocklists to generate Windows Firewall rules. This application fully blocks inbound and outbound traffic to or from IP addresses/address ranges/address ses found in IP blocklists
This application can never guarantee protection against all external threats. It blocks inbound traffic to, and outbound traffic from, your computer by creating firewall rules for all IP (V4 and V6) addresses 
found in more than 20 popular public IP address blocklists.
It no longer attempts to set rules with specific ports as 
	a) these are rarely provided and 
	b) Windows Firewall requires the protocol to be specified when the port(s) is/are specified. This has not been practical as none of the blocklists thus far have included this.
Protocols are also no longer specified as the blocklists do not provide this information.

## Current status (Version 1.7.0)
I have put a lot of effort into improving performance, reducing "Process All" performance from about an hour to roughly 15 minutes to process more than 460K blocklist entries, then a current runtime of 8 minutes using my fairly entry-level i5 with 8GB RAM and a 500GB M2 SSD.
Now my focus is on fresh detailed testing after the latest changes.

## If it works for you...
Buy me a coffee (US$ 5.00) if you feel that this app makes your life safer and easier!

## Main features
Directly reads IP address blocklists from the internet, decompresses them and processes them to create Windows Firewall rules.
The application can be run in two modes:
- **Interactive mode**: The user can select the blocklists to be processed and the action to be taken (add or remove rules).
- **Scheduled mode**: The application can be scheduled to run at regular intervals to update the blocklist rules. The user can select the blocklists to be processed by changing the command line parameters.

## .NET versions
This project was developed using .NET 8.0 and C# 12.0 and the current version uses .NET 9.0. Older .NET versions are not supported, especially .NET Framework versions.

## Operating systems and user interface
This version has a Windows Forms interface to help you to get started. It's neither the slickest nor most modern interface but then, it's not intended for frequent use.
It currently only manages rules in the Windows Defender Firewall and is therefore **not portable to other operating systems**. 
The interactive mode of this application requires Windows 11 and Windows 10. Theoretically, it **might** run on Windows 7, 8 or 8.1 but this has not been tested.
I would love to change it to manage blocklist rules at network firewall level but that would be a far more complex exercise so is not imminent.

## Dependencies
The project has the following direct dependencies:

### Microsoft frameworks
- Microsoft.NETCore.App
- Microsoft.Windows.SDK.NEt.Ref.Windows
- Microsoft.WindowsDesktop.App.WindowsForms

### Nuget packages
- Microsoft.EntityFrameworkCore version 9.0.1
- Microsoft.Extensions.Configuration.Json version 9.0.1
- SharpCompress version 0.39.0 [https://github.com/adamhathcock/sharpcompress](https://github.com/adamhathcock/sharpcompress)
- System.DirectoryServices.AccountManagement version 9.0.1
- TaskScheduler version 2.11.0 [https://github.com/dahall/TaskScheduler](https://github.com/dahall/TaskScheduler)
- WindowsFirewallHelper version 2.2.0.86 [https://github.com/falahati/WindowsFirewallHelper](https://github.com/falahati/WindowsFirewallHelper)
- IPAddressRange version 6.1.0 [https://github.com/jsakamoto/ipaddressrange](https://github.com/jsakamoto/ipaddressrange)

### API
- BlocklistAPI, hosted at MonsterASP.net. This is a RESTful API that accesses the few data tables used by this application. (Device, DeviceRemoteAddress, FileType and RemoteSite). This API is not intended for public use and is not documented. 
- Note that the only identifier in the Device table is the device's MAC address which is used to track last time downloaded as certain sites disallow too-frequent downloads (e.g. dan.me.uk)

### Other
- OSVersionExtension version 3.0.1, recompiled with .NET 8.0 and 9.0 [https://github.com/pruggitorg/detect-windows-version](https://github.com/pruggitorg/detect-windows-version), so is not the same version as the available Nuget version.
- SBS.Utilities.dll version 1.12.0.0, not intended for public use and is not documented.

## General

Whilst I have tested extensively and been running this application daily in the Windows Task Scheduler, I cannot guarantee that it is bug free! Applications will always have bugs and this one does some fairly complex processing, so is no exception.
Please feel free to notify me at unrealaddress@duck.com of any bugs found with as much supporting information as possible.
I recommend that this be scheduled to run at less busy times of the day as it still uses what I regard as excessive resources... but then, it's working to remove and add firewall rules blocking hundreds of thousands of IP addresses, IP address ranges and subnets.
I've tried different ways (netsh, powershell etc.) of working with the Windows 11 firewall. All take time to add or remove rules. Remember that even after de-duplicating addresses and consolidating them into batches of 1000, over 1000 firewall rules (inbound plus outbound) are created.
**NEW with version 1.6.0: Added conversion of IP address ranges to batches and increased the maximum batch size to 10,000 on Windows 11 and Windows Server 2022 machines. This has resulted in a big performance improvement **
**NEW with version 1.7.0: Improved the performance of combining addresses and ranges into sets**

## User manual
A short document providing guidance on general usage can be found in BlocklistManager.docx in the same folder as this README.

