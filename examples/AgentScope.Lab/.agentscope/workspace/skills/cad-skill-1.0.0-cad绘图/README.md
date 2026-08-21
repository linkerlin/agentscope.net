# cad_skill

A Windows local CAD control skill for OpenClaw / ClawHub.

Supports:
- solidworks
- catia
- creo
- ug

Main capabilities:
- launch_app
- open_file_in_app
- is_app_running
- close_app
- get_active_app
- get_running_apps
- detect_app_path
- set_app_path

Notes:
- does not scan the whole computer
- does not read registry
- only checks saved paths and predefined common paths