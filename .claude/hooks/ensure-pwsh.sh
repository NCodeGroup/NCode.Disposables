#!/usr/bin/env bash
#
# ensure-pwsh.sh - make PowerShell available in a Claude Code cloud session.
#
# This project's scripts and commands run with `pwsh`, and a cloud session's
# container doesn't have it, so a session would stop and ask to install it.
# session-start.sh in this folder runs this file instead, from the SessionStart hook in .claude/settings.json.
#
# It does nothing outside a cloud session (CLAUDE_CODE_REMOTE is not "true"),
# so it never installs software on a developer's own machine, and nothing when
# pwsh is already on the PATH, such as when the cloud environment's setup script
# installed it. Otherwise, on Ubuntu 24.04 x86_64 it installs the pinned
# PowerShell package from Microsoft's apt repository, which a cloud session's
# network reaches by default, after checking its SHA-256. Any other system, or
# any failure, prints one line and exits 0: the hook never blocks a session.
#
# To move to a newer PowerShell, change PWSH_VERSION and PWSH_SHA256 to the values of the
# `powershell` entry in
# https://packages.microsoft.com/ubuntu/24.04/prod/dists/noble/main/binary-amd64/Packages.gz
#
# This file comes from the project template, and template updates replace it.

[ "${CLAUDE_CODE_REMOTE:-}" = "true" ] || exit 0
command -v pwsh >/dev/null 2>&1 && exit 0

PWSH_VERSION=7.6.9-1.deb
PWSH_SHA256=491b1a51e257e2ac86af33e3a4dc9aa91adb1b3926a57967af39d9a1ca38dc79

os=$(. /etc/os-release 2>/dev/null && echo "${ID:-}:${VERSION_ID:-}:${PRETTY_NAME:-unknown}")
case "$os:$(uname -m)" in
    ubuntu:24.04:*:x86_64) ;;
    *)
        echo "ensure-pwsh: pwsh is missing and this system (${os#*:*:}, $(uname -m)) has no pinned package; install PowerShell 7 to run this project's scripts."
        exit 0
        ;;
esac

sudo=
if [ "$(id -u)" != 0 ]; then
    command -v sudo >/dev/null 2>&1 && sudo="sudo -n" || {
        echo "ensure-pwsh: pwsh is missing and this session is not root and has no sudo."
        exit 0
    }
fi

# env sets DEBIAN_FRONTEND for apt-get itself, which sudo would otherwise strip; the package lists are refreshed
# only when the first install fails, since a fresh container's lists may be empty or stale.
apt_install() { $sudo env DEBIAN_FRONTEND=noninteractive apt-get install -y "$dir/$file" >/dev/null; }

dir=$(mktemp -d) || exit 0
file="powershell_${PWSH_VERSION}_amd64.deb"
curl -fsS -m 120 -o "$dir/$file" "https://packages.microsoft.com/ubuntu/24.04/prod/pool/main/p/powershell/$file" &&
    echo "$PWSH_SHA256  $dir/$file" | sha256sum -c - >/dev/null &&
    { apt_install || { $sudo apt-get update -qq >/dev/null 2>&1 && apt_install; }; } &&
    echo "ensure-pwsh: installed PowerShell $(pwsh -NoProfile -Command '$PSVersionTable.PSVersion.ToString()')" ||
    echo "ensure-pwsh: installing PowerShell failed; the project's scripts need pwsh."
rm -rf "$dir"
exit 0
