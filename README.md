# RTS Multiplayer Game

This is a Real-Time Strategy, online multiplayer game which is similar to the "Clash Royale" game developed by [Supercell](https://supercell.com/en/). 

## Game description

In short, it's a match-based game, 1vs1, where players fight using a group of battle units defined by a deck configured before the match. In the battlefield, each player is represented by a castle. The goal of each player is to place the battle units strategically in the battlefield so they reach the enemy castle and destroy it. The game ends when a castle is destroyed or the match time runs out. The winner receives gold, trophies and has a chance of unlocking a new battle unit. The loser receives less gold and loses some trophies.

The main parts that define the software structure are the login screen, the main menu and the gameplay scene.

* ### Login screen

When launching the game, this is the interface shown if there is no user session active. It contains the Sign In and Sign Up functionalities, and the credits section which mentions the authors of the third-party assets used in the project.

<p align="center"><img src="/res/screenshots/login.png?raw=true" width=100% height=100%/></p>

* ### Main menu

The main menu is shown once the user is logged in. It provides access to the battle units and deck management, and to some application settings such as sound volume or FPS limit. It's also the interface from which the users can begin the matchmaking process.

<p align="center"><img src="/res/screenshots/main_menu.png?raw=true" width=100% height=100%/></p>

* ### Gameplay scene

Once the matchmaking process completes, players will enter the battlefield with their configured decks. From each player's perspective, the enemy castle is the red colored one. On the bottom side of the screen, players will see the castles HP bars and their units from the deck that are available to place on the battlefield. On the left side is shown the energy bar (each battle unit requires a fixed amount of energy to be played). On top is displayed the remaining match duration, one button to change the battlefield view perspective and one button for a little menu which lets the player either surrender or quit the application.

<p align="center"><img src="/res/screenshots/in_game.png?raw=true" width=100% height=100%/></p>

## Demo

References to all of the assets used are listed in the [assets.txt](https://github.com/ioan45/rts-multiplayer-game/blob/main/assets.txt) file. The Unity project in this repository doesn't contain all the assets listed there because of their licenses. Here's a [demo](https://www.youtube.com/watch?v=c4ki610-6cI) using all of the assets and online services, plus a name given to the game.

## Used Technologies

#### For the software development:
* [Unity Engine](https://unity.com/download) (editor version: 2021.3.18f1)

#### For the multiplayer functionality:
* [Netcode for GameObjects library](https://docs-multiplayer.unity3d.com/netcode/current/about/) (latest tested version: 1.2.0)
* [Game Server Hosting (Multiplay) service](https://unity.com/products/game-server-hosting) (unity package version: 1.0.5)
* [Matchmaker service](https://unity.com/products/matchmaker) (unity package version: 1.1.1)

#### For the web server and database (through a hosting service):
* [PHP](https://www.php.net/) (v8.1)
* [MySQL](https://www.mysql.com/)

#### For the battle units movement AI:
* [A* Pathfinding Project](https://arongranberg.com/astar/) (v4.2.17)

## Technical features

1. Using multiplayer server hosting and matchmaking cloud services to connect players from all over the world.
2. User accounts with their data being stored in a cloud database.
3. Fast application launching thanks to user sessions.
4. Repeated automatic reconnection attempts when disconnected from the match.
5. Repeated automatic reconnection attempts when the application is reopened after being closed while in match (if the match is still active).
6. Checking, at a fixed frequency, the availability of the web server via ping requests (except during match sessions).

## Implemented security measures

1. Automatically redirecting requests to the web server from HTTP to HTTPS via configuration in a `.htaccess` file.
2. All requests to the web server are redirected to the `index.php` script (via configuration in a `.htaccess` file) which decides what script to call based on the URL of the request.
3. SQL Injection prevention: prepared statements, converting strings to integer values, "escaping" strings.
4. In the database, storing user passwords in their hash form using the bcrypt algorithm (via the API available in PHP).
5. User input validation, both in the application and on the web server.
6. Using a dedicated server model for the multiplayer sessions.
7. Using the server-authoritative multiplayer architecture.
8. Players connect to the match server using a password generated for that match. While the match is active, the password is stored in the database so players can reconnect when reopening the application.
9. Once the players connect for the first time on the match server, they will be remembered by the server so only they can connect to it.

## Technical design

* ### Scene flow in Unity engine
The scene structure in the application can be thought of as two layers. The base layer is represented by a scene named "Core" which is always active and the other layer is represented by the actual scenes of the application (login screen, main menu, match session) where only one of these scenes is active at a time.

The application starts with the Core scene which contains the camera, the light source and some management objects provided to the rest of the application via the "Singleton" pattern. After the Core scene completes its initialization, it loads the login screen scene which checks for active user session and, depending on that, decides what second layer scene should actually be active at that moment.

The following diagram shows the checking done by the login screen scene at startup:

<p align="center"><img src="/res/diagrams/user_session_processing.png?raw=true" width=70% height=70%/></p>

The second layer scene switching is accomplished via a call to one of the managers present in the Core scene. The switching flow accomplished by the call looks like this: 

1. Show the loading screen.
2. Unload the current active scene.
3. Load the new scene and set it as active.
4. Hide the loading screen.

* ### Matchmaking flow

The matchmaking process can be described by the following diagram:

<p align="center"><img src="/res/diagrams/matchmaking_flow.png?raw=true" width=85% height=85%/></p>

It starts on pressing the "Find Match" button and ends when it's cancelled or when the match starts. The process consists of repeated iterations until one of the end conditions is met. Each iteration consists of a call to the matchmaker service (the "Find opponent" step in the diagram) that returns the match server connection data on success. Once the call succeeds, the process runs some attempts to obtain the server password from the database (the password is generated by the server application at startup) and on success it will prepare the connection payload and will begin attempting the connection. If any of the iteration steps fails, the process will begin a new iteration.

Thanks to the fact that both the matchmaking and the server hosting services are from Unity, they can easily interact with each other. On the matchmaker service call step in the process, matchmaker will wait until a second player does the same thing. Once that happens, matchmaker requests a server allocation from the hosting service and on success it will send the connection data (IP & port) to both players, thus completing the service call step in the process. If the allocation part was successful, the hosting service will launch the server application which, at startup, will generate the server password so both players can get it from the database.

* ### Database and web server

The database is represented by the following diagram:

<p align="center"><img src="/res/diagrams/database.png?raw=true" width=85% height=85%/></p>

The communication between the application and the database is done via the web server. The application does web requests using the `UnityWebRequest` class of the `UnityEngine.Networking` namespace and waits in an asynchronous manner for the response. The web server sends SQL queries to the database server by using [MySQLi](https://www.php.net/manual/en/book.mysqli.php). The response returned by the web server to the application respects a certain format. The first thing in the response is the operation status, followed by the actual data of the response. Elements in the response use separators which are characters not found in the response data like '\t' or '&'. For example, here's a reponse which contains the player name, a list of the owned battle units (referenced by their ids) and a list of their levels (spaces are used just for readability): `1 \t alexandruCulea \t 1 & 2 & 3 & 4 \t 2 & 1 & 5 & 10`. The '1' status signals success.

## Installation

1. Clone the repository into your system. You can use the command `git clone https://github.com/ioan45/rts-multiplayer-game.git`.
2. Get the database and the web server online. The hosting service I used to make the demo is [000WebHost](https://www.000webhost.com/). Once you get the database online, complete the [DbConnectionData.php](https://github.com/ioan45/rts-multiplayer-game/blob/main/src/web-server/DbConnectionData.php) script with the database connection data. Also, the `index.php` script sets the default time zone, so you might want to change that. After that, put the contents of [this](https://github.com/ioan45/rts-multiplayer-game/tree/main/src/web-server) directory in the root directory of your hosted web server. If your hosting solution provides a `.htaccess` file in the root of the server then don't overwrite it and just add at the end of it the contents of the file provided in this project.
3. In the application C# scripts, search for every URL string constant that refers to the online web server and complete them with the domain of your hosted web server. You can find them all by searching in the files for `https:// - /`.
4. Download the missing assets that are necessary for the project to run. Those are [TextMesh Pro](https://learn.unity.com/tutorial/working-with-textmesh-pro#) and [A* Pathfinding Project](https://arongranberg.com/astar/). A TextMesh Pro pop-up for installing it should appear the first time you open the Unity project. As for the A* Pathfinding Project, at the time of writing, the source website lets you download a unity package to import into the project. Regarding A* Pathfinding Project, the three missing script components in the prefabs of the battle units and the one missing in a GameObject of the Gameplay scene are caused by the absence of the asset. So, installing it should solve the problem. Also, if you choose to install it, uncomment the code in the `CombatUnitBehaviour.cs` and `MinionBehaviour.cs` scripts.
5. (Optional) Download the battle units models and animations, and add them to the battle units prefabs. Anyway, the project should run without completing this step because there are capsule models used instead.
6. Set up the [Multiplay](https://unity.com/products/game-server-hosting) and [Matchmaker](https://unity.com/products/matchmaker) services for this project. They provide the guide for this. Details on building the server application to upload on Multiplay can be found at the [Build](https://github.com/ioan45/rts-multiplayer-game#build) section. Besides the default services configurations you should:
    * For Multiplay, add those to the server build arguments: `-serverip $$ip$$ -nographics -batchmode`.
    * For Matchmaker, the default queue with one pool it's enough and should be just one player per ticket. Also, regarding pool rules, it should be two teams for a match, each team having exactly one player.

Now you should be able to make a functional client build which you can give to your friends and have fun. Check the [Build](https://github.com/ioan45/rts-multiplayer-game#build) section for making the build.

## Local testing

The unity project scripts are using two scripting symbols: `BYPASS_UNITY_SERVICES` and `USING_LOCAL_SERVERS`. Those are set up in the project settings. 

If `BYPASS_UNITY_SERVICES` is defined, Multiplay and Matchmaker services are not used. Instead, the "Find Match" button will try to connect to a match server already launched on localhost. Also, if this is defined, when you launch the application, before anything else you will be asked if the application should run as a client or match server.

If `USING_LOCAL_SERVERS` is defined, the online web server is not used. Instead will be used a web server on localhost. You can still use a cloud database if you want, that's because the database connection data is stored in a PHP script on the web server, the game application doesn't know about it. If you want to use a local database then just update the constants in that PHP script. During the development of the game I used [Apache HTTP Server](https://httpd.apache.org/) as local web server solution.

Using those two scripting symbols you can play/continue development of the game without needing to access any cloud service. You can run the multiplayer part locally by having three game builds launched at the same time (or 3 instances of the project with the editor on Play mode): one which runs as the match server and two as the clients (players). The match server one should be already launched when pressing the "Find Match" button from the clients. For easier development, you can have the original project and the other two copies linked to the original via symbolic links.

## Build

The application has two types of builds: one for the clients and one for the multiplayer server. The client build is for the Windows platform while the server one is for the Linux platform. 

When making the server build, in the Build Settings make sure you select "Dedicated Server" from the platform list and "Linux" from the target platform dropdown menu.

For both types, make sure there are four scenes listed in the Build Settings: Core, Login, MainMenu and Gameplay. Also, make sure that the Core scene is the first one listed there because that should be the scene loaded on startup.

The project has two editor scripts. One is executed just before the build starts and has the job of removing the defined scripting symbols (`BYPASS_UNITY_SERVICES` and `USING_LOCAL_SERVERS`). The other one is executed after the build completes to define the scripting symbols back. That way, you don't need to manually remove the symbols when building the application.
