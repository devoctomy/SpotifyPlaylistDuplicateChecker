using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace SpotifyPlaylistDuplicateChecker
{
    class Program
    {
        private static bool _includePrivate = true; // Requires playlist-read-private scope
        private static bool _onlyMyPlaylists = true;

        static async Task<int> Main(string[] args)
        {
            //Get your access token using the Spotify developer hub
            //when testing api calls via green 'GET TOKEN' button
            //https://developer.spotify.com/console/get-track/
            var spotifyAccessToken = Environment.GetEnvironmentVariable(
                "SPDC_AccessToken",
                EnvironmentVariableTarget.User);
            
            if(!string.IsNullOrEmpty(spotifyAccessToken))
            {
                var spotify = new SpotifyClient(spotifyAccessToken);
                var user = await spotify.UserProfile.Current();

                Console.WriteLine("Getting user playlists.");
                var allPlaylists = await spotify.Playlists.CurrentUsers();

                Console.WriteLine("Checking for duplicates.");
                var allDuplicates = new Dictionary<string, List<string>>();
                var artistCount = new Dictionary<string, int>();
                foreach (var curPlaylist in allPlaylists.Items)
                {
                    var playlist = await spotify.Playlists.Get(curPlaylist.Id);
                    if(!playlist.Public.GetValueOrDefault() && !_includePrivate)
                    {
                        continue;
                    }

                    if(playlist.Owner.Id != user.Id && _onlyMyPlaylists)
                    {
                        continue;
                    }

                    Console.WriteLine($"Checking playlist {playlist.Name}.");
                    foreach (var curTrack in playlist.Tracks.Items)
                    {
                        var fullTrack = curTrack.Track as FullTrack;

                        foreach(var curArtist in fullTrack.Artists)
                        {
                            if (artistCount.ContainsKey(curArtist.Name))
                            {
                                artistCount[curArtist.Name] = artistCount[curArtist.Name] + 1;
                            }
                            else
                            {
                                artistCount.Add(curArtist.Name, 1);
                            }
                        }

                        var displayName = string.Join(',', fullTrack.Artists.Select(x => x.Name).ToArray()) + " - " + fullTrack.Name;
                        var foundIn = new List<string>();
                        if(allDuplicates.ContainsKey(displayName))
                        {
                            foundIn = allDuplicates[displayName];
                        }
                        else
                        {
                            foundIn = new List<string>();
                            allDuplicates[displayName] = foundIn;
                        }

                        foundIn.Add(playlist.Name);
                    }
                }

                Console.WriteLine("-------------------");
                Console.WriteLine("Duplicate results...");
                var duplicates = allDuplicates.Where(x => x.Value.Count > 1).ToList();
                if(duplicates.Count > 0)
                {
                    foreach (var curDuplicate in duplicates)
                    {
                        Console.WriteLine($"'{curDuplicate.Key}' found '{string.Join(',', curDuplicate.Value)}'");
                    }
                }
                else
                {
                    Console.WriteLine("No duplicates found.");
                }

                Console.WriteLine("-------------------");
                Console.WriteLine("The top 10 artists are...");
                var orderedArtists = artistCount.ToList().OrderByDescending(x => x.Value);
                foreach (var curArtist in orderedArtists.Take(10))
                {
                    Console.WriteLine($"{curArtist.Key} appeared {curArtist.Value} times.");
                }
            }
            return 0;
        }
    }
}
