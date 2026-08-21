---
name: cad_skills
description: Control local CAD applications on Windows including launching apps, opening files, checking status, closing apps, detecting active or running apps, detecting common executable paths, and saving user-provided executable paths.
---

# CAD Skill

This skill allows an AI assistant to control locally installed CAD applications on Windows.

Supported software currently includes:

- solidworks
- catia
- creo
- ug (Siemens NX)

The skill enables launching CAD software, opening files with specific applications, checking running status, closing applications, and detecting active CAD windows.

This skill is intended for **local workstation automation**.

---

# Important Constraints

The assistant must follow these constraints when using this skill:

1. Do not scan the entire computer filesystem.
2. Do not traverse all directories on disk.
3. Do not read the Windows registry.
4. Only use paths stored in `config.json`.
5. Only check predefined candidate install paths.

If the executable cannot be located:

- ask the user to provide the full executable path
- then call `set_app_path` to store the path.

---

# Supported Applications

Currently supported CAD software:

- solidworks
- catia
- creo
- ug

---

# Execution

All commands are executed through:

skill_runner.py

Input payload format:

```json
{
  "skill": "launch_app",
  "args": {
    "app": "solidworks"
  }
}
```



# Supported Actions

## launch_app

Launch a CAD application.

Arguments:

- app

- config_file (optional)

##  open_file_in_app

Open a file using a specified CAD application.

Arguments:

- app
- file_path
- config_file(optional)
- auto_launch(optional)
- wait_seconds(optional)

##  is_app_runing

Check whether a CAD application is currently running.

Arguments:

- app
- config_file(optional)

##  close_app

Close a CAD application.

Arguments:

-  app
- config_file(optional)
- force(optional)

##  get_activate_app

Detect which CAD application window is currently active.

Arguments:

-  config_file(optional)

##  get_running_apps

Return all currently running supported CAD applications.

Arguments:

- config_file(optional)

##  detect_app_path

Detect an executable path using only predefined paths and saved paths.

Arguments:

- app
- config_file(optional)

## set_app_path

Save a user provided executable path into the configuration file.

Arguments:

- app
- path
- config_file(optional)

# Example Usage

Launch SolidWorks:

```json
{
  "skill": "launch_app",
  "args": {
    "app": "solidworks"
  }
}
```

open a STEP file in SolidWorks:

```json
{
  "skill": "open_file_in_app",
  "args": {
    "app": "solidworks",
    "file_path": "D:\\DESKTOP\\solvi_project\\moca\\moca.STEP",
    "auto_launch": true,
    "wait_seconds": 5
  }
}
```

Save a user provided executable path:

```json
{
  "skill": "set_app_path",
  "args": {
    "app": "solidworks",
    "path": "E:\\Program Files\\SOLIDWORKS Corp\\SOLIDWORKS\\SLDWORKS.exe"
  }
}
```

# Note

This skill is designed for **local CAD workstation automation**.

It intentionally avoids heavy filesystem scanning to maintain fast response and predictable behavior.