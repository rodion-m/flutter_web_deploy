# Flutter Web Deploy
This tool allows to deploy flutter web app via FTP with one click. At the moment the program **works only from Windows**.

# Installiation
Compile a project using VS, Rider or `dotnet publish` OR **download a compiled executable from [releases](https://github.com/rodion-m/flutter_web_deploy/releases)**.<br>
Then put the tool (`FlutterWebDeploy.exe`) into your flutter project directory (also you can create another folder and put the program here, for example `project_dir/deployment`).
After that you'll have two ways to use it (below).
### Option 1: Config file
Just open (or create) file `flutter_web_deploy.yaml` (it should be placed inside the program directory). And set the required properties: `ftp_login`, `ftp_password`, `ftp_host` and `remote_path`.

### Option 2: Program arguments
The second variant is to set options in the program arguments. Here is the main arguments list:
```
  -l, --login       FTP connection login.

  -p, --pass        FTP connection password.

  -h, --host        FTP host started with 'ftp.'

  --port            FTP connection port (21 is default).

  -f, --fast        Upload only main project files (skip images and etc.)
```

*Run `FlutterWebDeploy.exe --help` to see all possible arguments.*

# Deploy
After configuring you can run `FlutterWebDeploy.exe` manually or add it as a configuration into Android Studio.<br>
1. Open `Edit Configurations` window, click add button, select `Shell Script`<br>
2. Set path to `FlutterWebDeploy.exe` into `Script path` field and path to project into `Working directory` field<br>
3. Optionally add program arguments (`Script options` field).<br>
![Android Studio Configurations](/images/android_studio_configuration.png)<br>
Congratulations! Now you can publish your application with a one click.
![Android Studio run](/images/android_studio_run.png)<br>
# Questions
- Q: How to add custom arguments to `flutter build web`?
- A1: Just specify your custom arguments in `flutter_web_deploy.yaml` (`flutter_custom_arguments` property). Also you can set built-in options in `flutter_arguments` area:
```
flutter_arguments:
  no_sound_null_safety: true
```
- A2: Or add your custom flutter arguments into a program arguments after `--`, like this: `FlutterWebDeploy.exe --custom_args -- --profile`

- Q: Is it possible to use a config file combined with command line arguments?
- A: Yes, it is. Note that command line arguments are always has a priority.

# ToDo
- MacOS and Linux platform support.<br>
---
The tool is developed under .NET Core.
