# Git and GitHub on Mac

The destination is https://github.com/reachus-sreeni/service-connect, branch `main`. The application has been published to main; see the verification report for build and test results.

## Push the prepared local checkout

The `outputs/service-connect` folder delivered with this work is already a Git checkout on `main`, based on the repository’s existing history, with the application committed. After installing Git and GitHub CLI and completing `gh auth login`, open Terminal in that folder and run:

```sh
git status
git push -u origin main
```

If you have only the source ZIP, use the ZIP instructions below; the ZIP intentionally excludes Git metadata.

## Install Git

```sh
git --version
```

If missing, run `xcode-select --install`, follow the installer and try again. Official guidance: https://git-scm.com/download/mac.

## Clone the published project

```sh
git clone https://github.com/reachus-sreeni/service-connect.git
cd service-connect
git switch main
```

Set your actual name and GitHub email (or the no-reply email in GitHub Settings → Emails):

```sh
git config user.name 'YOUR NAME'
git config user.email 'YOUR GITHUB EMAIL'
```

## Authenticate for future pushes

A public clone does not require authentication. Pushing does. Install GitHub CLI from https://cli.github.com, then:

```sh
gh auth login
```

Choose GitHub.com, HTTPS, and browser login, and agree to authenticate Git. Never paste a token or password into chat or put it in the repository URL. GitHub account passwords do not work for Git HTTPS authentication. See https://docs.github.com/en/get-started/git-basics/caching-your-github-credentials-in-git.

## Publish future edits

```sh
git pull --ff-only origin main
git status
git add README.md frontend/src backend/ServiceConnect.Api docs tests
git diff --cached
git commit -m 'Update Service Connect'
git push origin main
```

Stage specific changed paths as needed, inspect them and confirm that no secrets are included. If a push is rejected because the remote moved, run `git fetch origin`, inspect `git log --oneline --all --graph`, and reconcile the changes before pushing. Do not force-push main.

## If starting from the ZIP instead

Clone the existing repository first using the commands above, then copy the ZIP's project contents into that clone (including `.github`, `.gitignore` and `.env.example`, but excluding `.git`, `.env`, `node_modules`, `bin` and `obj`). Review `git status` and commit/push. This preserves existing GitHub history and avoids creating an unrelated local history.
