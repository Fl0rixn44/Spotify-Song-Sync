<!-- ABOUT THE PROJECT -->
## Spotify Song Sync

Have you always wanted to listen to synchronized music with friends? And on one of the best music streaming platforms? Then this is just the thing for you and your friends.

Spotify Song Sync offers 1:1 synchronization with your current playback.

Thanks to [qodNils](https://steamcommunity.com/profiles/76561198290200683/) there is currently a server online that already allows you to synchronize with songs. Simply download and start! If you want to host the whole thing yourself, the server is included in the source.

## How does it work?

Spotify Song Sync uses the official Spotify API and a TCP client to enable synchronization. The Spotify API is used to retrieve which song you are currently listening to and to influence your Spotify player. As you can see in the source, only the current song and its data is queried as well as your current authenticated user to display things like your username or profile picture if available. The TCP client is there to establish an active exchange between you and the party creator. No IP addresses are exposed to others for this purpose.

<!-- GETTING STARTED -->
## Getting Started

To get a local copy up and running follow these steps.

### Prerequisites

* .NET Runtimes
* Active Internet connection
* May need Spotify Premium in order to skip Ads

### Installation

1. Download [latest release](https://github.com/Fl0rixn44/Spotify-Song-Sync/releases/latest)
2. Unzip release to desired install folder
3. Start the application and go to the settings
4. Follow the instructions there

## Usage

Start or join a party, you just need to have Spotify open on a device and already authenticated with your own app.

<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE` for more information.

__This is a standalone project from Fl0rixn.  Spotify does not endorse or sponsor this project.__  

<!-- CONTACT -->
## Contact

Discord - fl0rixn
