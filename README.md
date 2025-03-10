# BlocklistManager

This Microsoft Windows application manages Windows Firewall rules for IP addresses found in IP address blocklists. It can be run interactively or be scheduled to run at regular intervals. 

- Interactive mode: Run the program without any command line parameters to open the Windows Forms interface.

- Command line mode: Run the program with the following command line parameters:
  - `/sites:` followed by a semicolon-separated list of site IDs to specify the blocklists to be processed, e.g. '/sites:1;2;3'. Enter the value 'allCurrent' to process all active blocklists (e.g. '/sites:allCurrent').
  - `/logpath:` followed by the path to the log file (e.g. '/logpath:C:\Logs').

**NOTE: The application needs to be run with administrator privileges to be able to change Windows Firewall rules.**

I have also included a general usage guide in the file BlocklistManager.docx in the same folder as this README.

## Application objective

To enhance computer security using popular IP address blocklists to create Windows Firewall rules. This application blocks all traffic to or from IP addresses/address ranges/addresses found in blocklists

This application can never guarantee protection against all external threats. It blocks inbound traffic to, and outbound traffic from, your computer by creating firewall rules for all IP (V4 and V6) addresses 
found in more than 20 popular public IP address blocklists.

It no longer attempts to set rules with specific ports as 
	a) these are rarely provided; and 
	b) Windows Firewall requires the protocol to be specified when the port(s) is/are specified. This has not been practical as none of the blocklists thus far have included both details.

Protocols are no longer specified as none of the current blocklists provide this information.

## Current status (Version 1.8.3)

I have put a lot of effort into improving performance, reducing "Process All" performance from about an hour to under 10 minutes to process more than 600,000 blocklist entries, then a current runtime of 
under 5 minutes using a 10 year old entry-level i5 with 4GB RAM and a 500GB SSD...but the overall run time is mostly dependent on the time taken to remove and add firewall rules. Downloading the blocklists and
preparing the firewall rules takes less than 1 minute.

## If it works for you...

Buy me a coffee (https://buymeacoffee.com/cedriccullingworth) if you feel that this app makes your life safer and easier!

## Main features

Directly reads IP address blocklists from the internet, decompresses and processes them to create Windows Firewall rules.

The application can be run in two modes:
- **Interactive mode**: The user can select the blocklists to be processed and the action to be taken (add or remove rules).
- **Scheduled mode**: The application can be scheduled to run at regular intervals to update the blocklist rules. The user can select the blocklists to be processed by changing the command line parameters.

## .NET versions

This project was developed using .NET 8.0 and C# 12.0 and the current version uses .NET 9.0. Older .NET versions are not supported, especially .NET Framework versions.

## Operating systems and user interface

This version has a Windows Forms interface to help you to get started. It's neither the slickest nor most modern interface but then, it's not intended for frequent use.

It currently only manages rules in the Windows Defender Firewall and is therefore **not portable to other operating systems**. 

The interactive mode of this application requires Windows 10 or 11. Theoretically, it **might** run on Windows 7, 8 or 8.1 but this has not been tested.

I would love to change it to manage blocklist rules at network firewall level but that would be a far more complex exercise so is not imminent.

## Dependencies and credits

The project has the following direct dependencies:

### Microsoft frameworks
- Microsoft.NETCore.App
- Microsoft.Windows.SDK.NET.Ref.Windows
- Microsoft.WindowsDesktop.App.WindowsForms

### Nuget packages
- IPAddressRange version 6.1.0 [https://github.com/jsakamoto/ipaddressrange](https://github.com/jsakamoto/ipaddressrange)
- Microsoft.Extensions.Configuration.Json version 9.0.2
- SharpCompress version 0.39.0 [https://github.com/adamhathcock/sharpcompress](https://github.com/adamhathcock/sharpcompress)
- System.DirectoryServices version 9.0.2
- System.DirectoryServices.AccountManagement version 9.0.2
- TaskScheduler version 2.12.1 [https://github.com/dahall/TaskScheduler](https://github.com/dahall/TaskScheduler)
- WindowsFirewallHelper version 2.2.0.86 [https://github.com/falahati/WindowsFirewallHelper](https://github.com/falahati/WindowsFirewallHelper)

### External API
- BlocklistAPI, hosted at at a developer host site. This is a RESTful API that accesses the few data tables used by this application. (Device, DeviceRemoteAddress, FileType and RemoteSite). 
  This API is not intended for public use and is not documented. 
- Note that the only stored identifying information is your device's MAC address which is only used to track last time downloaded per site as certain sites disallow too-frequent downloads (e.g. dan.me.uk)

### Other
- OSVersionExtension version 3.0.1, recompiled with .NET 9.0 [https://github.com/pruggitorg/detect-windows-version](https://github.com/pruggitorg/detect-windows-version), so is 
not the same as the available Nuget version.

## General

Whilst I have tested extensively and been running this application daily in the Windows Task Scheduler for some time, I cannot guarantee that it is bug free! 
Applications will always have bugs and this one does some fairly complex processing, so is no exception.

Please feel free to notify me at unrealaddress@duck.com of any bugs found with as much supporting information as possible.

I recommend that this be scheduled to run at less busy times of the day as it still uses what I regard as excessive resources... but then, it's working to remove and add firewall rules blocking 
hundreds of thousands of IP addresses and IP address ranges.

I've tried different ways (netsh, powershell etc.) of working with the Windows 11 firewall. All of them take time to add or remove rules. Remember that even after de-duplicating addresses and consolidating them 
into batches of 1,000 (now 10,000 in Windows 11/Windows Server 2022), over 1,000 (in older operating system versions) firewall rules (inbound plus outbound) are created.

This app currently removes existing firewall rules per blocklist download site and then creates fresh rules based on the latest blocklist. Please let me know if you become aware of any valid reason to 
change this approach.

## User manual

A short document providing guidance on general usage can be found in BlocklistManager.docx in the same folder as this README.

