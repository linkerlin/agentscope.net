# -*- coding: utf-8 -*-
import json
import sys

from cad_launcher import (
    launch_app,
    open_file_in_app,
    is_app_running,
    close_app,
    get_active_app,
    get_running_apps,
    detect_app_path,
    set_app_path
)


def read_payload() -> dict:
    if len(sys.argv) > 1:
        raw = sys.argv[1]
    else:
        raw = sys.stdin.read()

    raw = raw.strip()
    if not raw:
        return {}

    return json.loads(raw)


def main():
    payload = read_payload()
    skill = payload.get("skill")
    args = payload.get("args", {})

    if skill == "launch_app":
        result = launch_app(
            app=args.get("app", ""),
            config_file=args.get("config_file", "config.json")
        )

    elif skill == "open_file_in_app":
        result = open_file_in_app(
            app=args.get("app", ""),
            file_path=args.get("file_path", ""),
            config_file=args.get("config_file", "config.json"),
            auto_launch=args.get("auto_launch", True),
            wait_seconds=args.get("wait_seconds", 3.0)
        )

    elif skill == "is_app_running":
        result = is_app_running(
            app=args.get("app", ""),
            config_file=args.get("config_file", "config.json")
        )

    elif skill == "close_app":
        result = close_app(
            app=args.get("app", ""),
            config_file=args.get("config_file", "config.json"),
            force=args.get("force", True)
        )

    elif skill == "get_active_app":
        result = get_active_app(
            config_file=args.get("config_file", "config.json")
        )

    elif skill == "get_running_apps":
        result = get_running_apps(
            config_file=args.get("config_file", "config.json")
        )

    elif skill == "detect_app_path":
        result = detect_app_path(
            app=args.get("app", ""),
            config_file=args.get("config_file", "config.json")
        )

    elif skill == "set_app_path":
        result = set_app_path(
            app=args.get("app", ""),
            path=args.get("path", ""),
            config_file=args.get("config_file", "config.json")
        )

    else:
        result = {
            "success": False,
            "message": f"未知 skill: {skill}"
        }

    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()