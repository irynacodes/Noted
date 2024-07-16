# Noted 

## What is it?

Noted is a very... unique application. Unique not in its idea, but in its execution. It is a music streaming platform that runs completely and totally in the console. Unconventional and limiting, sure, but it uses this limitation to its advantage – the simple, straightforward interface is impossible to get lost in.

## How did it come about?

I created this application as my final project for a C# class (PV178 at Masaryk University). The topic for the project left a lot of room for creativity – the only two requiremenets were:

- work with persistent storage (whether database or filesystem)
- make use of multithreading or asynchronous programming

Other than that, the topic and type of project were completely up to us. The reference topics were expense and tournament managers, but I knew that none of that would satisfy my need for my work to be **fun**. A Spotify knockoff, I thought, would do it. And it did! This was pretty much the first project I have ever created on my own (or ever, for that matter), four semesters after writing my first ever Hello World program 🌍

## What exactly can it do?

Though Noted appears simple, it actually offers a broad range of functionalities.

### Streaming

The main functionaity of any music streaming app is, well, streaming. And Noted does that quite alright! Users can:
- view all songs available on the platform
- view songs that they have added to their library
- view song suggested to them by Noted based on music tastes of their friends

Any song can be:
- viewed (its title, artist and album are displayed)
- added/removed to/from library
- added/removed to/from a playlist
- played (currently only supported by MacOS)

Any playlist or library can be:
- shuffled (a random song will start playing)

### Social

Like any fun streaming platform, Noted supports and promotes user interactions. Any user can:
- view their own profile (that they can edit, view their friends or blocked users)
- add another Noted user to friends (which enables them to view their library)
- block another Noted user (which removes them from friends, thus hiding their library from the user)
- access their song suggestions that are based on what their friends are listening to

## What does it look like?

<img width="717" alt="image" src="https://github.com/user-attachments/assets/d6d7016f-b717-450b-aa63-c676223ba3ed">
<img width="717" alt="image" src="https://github.com/user-attachments/assets/1c60a971-a0a8-420c-a99a-60182466198f">
<img width="717" alt="image" src="https://github.com/user-attachments/assets/ddecee63-834f-4a12-997a-75654a5470eb">

## How do I use it?

If you are using an IDE (e.g. Rider, Visual Studio), all you need to do is run the project with one button click – and you are good to go. If you would like to run the project directly from your built-in terminal console, then the process is a little less straightforward, but trivial nevertheless. The steps are:
1. make sure you have .NET SDK installed on your machine (`dotnet --version`)
2. navigate to the project's directory
3. build the project (`dotnet build`)
4. run the project (`dotnet run`)
