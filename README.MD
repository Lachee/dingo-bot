# Dingo Bot
A Rainbow Six Siege Bot

## Installation
### Requirements: 
1. DotNet Core 2.0
2. A Redis Service (can be remote)
3. [WkHtmlToPDF](https://wkhtmltopdf.org/downloads.html) _recommend to use the MXE for windows_

### Redis
Just setup a redis service somewhere. It is used for caching basically everything such as guild settings and reponse messages.

### WkHtmlToPDF
#### Windows
1. Download the [WkHtmlToPDF](https://wkhtmltopdf.org/downloads.html) zip file.
2. Extact its contents somewhere memorable. I extracted mine to D:/wkhtmltox
3. Make note of where the wkhtmltoimage.exe is located. We need to store this path later.

#### Ubuntu
1. Run `sudo apt-get install xvfb libfontconfig wkhtmltopdf`
2. Make sure it works. realpath is a good tip.

### The Bot
To run the bot, simply use `dotnet run --project DingoBot`. EZ.
When you first run it, a configuration file will be generated. Depending if you are using Visual Studio for debugging or DotNet to run it, the configuration file will be placed in different locations. **It is wherever the current working directory is**. It is called `config.json` by default.

Open this configuration up. You will see its a JSON file with a variety of options:

**TokenFile** is the relative file that stores the Discord Bot Token. This file is excluded from the git repository.

**Resources** is the relative path to the Resources folder. 

**Redis** is the connection details to the redis

Once these are all configured, run the bot again and it should connect just fine.
