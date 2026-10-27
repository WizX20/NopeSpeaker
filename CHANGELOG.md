# Changelog

All notable changes to xBlueTooth are listed here, newest first. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions are [semantic](https://semver.org/).
New entries go in [changelog.d/](changelog.d/README.md), one file per pull request - the release folds them in here under the new version and date.

## [Unreleased]

### Added

- added: the device list - every paired Bluetooth device, connected ones first, with whether Windows may play audio on it
- added: a switch per audio device that disables or enables its audio nodes (one UAC prompt per change), so Windows stops moving the sound to it when it connects
- added: a declined or failed change says why, with the errors of the elevated script
