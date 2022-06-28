FreeMarket OneX - Decentralized Anonymous Marketplace
=====================================================

Project web site: [FMONEX](https://fmone.org/)

Current version: 1.1 ([GitHub link](https://github.com/XRCPlatform/fmonex/releases))


## About FreeMarket OneX

Open-source, tor-based, peer-to-peer trading platform. The best way to trade goods & services worldwide, completely anonymously.

## About Source Code

FreeMarket OneX is the full node for FreeMarket OneX network. It is developed in C#, using the .NET Core platform.

[.NET Core](https://dotnet.microsoft.com/en-us/) is an open source cross platform framework and enables the development of applications and services on Windows, macOS and Linux.

Join our community on [Discord, Telegram, Twitter, ..](https://www.xrhodium.org/En/Community).

## Installation and setup from source code

.NET Core is required to build and run the node software. The installation and setup notes below have been tested on Ubuntu 20.04+. There is a convenience wrapper around most processes is provided to make setup quick.

**Follow full installation process at https://github.com/XRCPlatform/xrhodiumnode/wiki.**

 1. Clone the repository:

```
    git clone -b https://github.com/XRCPlatform/fmonex
    cd fmonex
	
	git submodule update --init --recursive
```

The `master` branch is bleeding-edge. Use this at your own risk.

 2. **Install .NET Core (dotnet-sdk-3.1)**. Follow instructions here: 
 https://docs.microsoft.com/en-us/dotnet/core/install/linux.


 3. Run It
 
 ```
    cd FreeMarketApp
    dotnet restore
	dotnet build
	dotnet run
```

## Further Information

Documentation is available at https://github.com/XRCPlatform/fmonex/wiki.

## Legal

See LICENSE for details.
