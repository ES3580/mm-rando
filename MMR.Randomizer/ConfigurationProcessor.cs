using MMR.Randomizer.Models;
using MMR.Randomizer.Models.Settings;
using MMR.Randomizer.Utils;
using MMR.Randomizer.Constants;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MMR.Randomizer.GameObjects;
using System.Xml.Schema;
using MMR.Randomizer.Models.Colors;
using System.Diagnostics;

namespace MMR.Randomizer
{



    public static class ConfigurationProcessor
    {
        public static string Process(Configuration configuration, int seed, IProgressReporter progressReporter)
        {
            int player_count = Int32.Parse(configuration.GameplaySettings.sPlayerCount);
            System.Diagnostics.Debug.WriteLine("Entering Process");
            if (!Directory.Exists(Path.Combine(Values.MainDirectory, "Resources")))
            {
                System.Diagnostics.Debug.WriteLine("Please extract the entire randomizer archive, including the Resources/ folder and subfolders");
                return $"Please extract the entire randomizer archive, including the Resources/ folder and subfolders";
            }
            /**************
             * 
             * Theory: calling for loop over 
             * var randomizer = new Randomizer(configuration.GameplaySettings, seed);
             * 
             * changing to array of randomizer
             * 
             * 
             * **************/

            //loop for player count times
            System.Diagnostics.Debug.WriteLine("Process() with playercount = " + configuration.GameplaySettings.sPlayerCount + " should be at 1<=x<=255");
            List<Randomizer> players_randomizer = new List<Randomizer>();
            List<RandomizedResult> players_randomized = new List<RandomizedResult>();
            for (int player = 0; player < player_count; player++)
            {
                System.Diagnostics.Debug.WriteLine("Building player Randomizer for player: " + player.ToString());
                players_randomizer.Add(new Randomizer(configuration.GameplaySettings, seed, player));
                System.Diagnostics.Debug.WriteLine("Appending null to players_randomized list for player: " + player.ToString());
                //original RandomizedResult randomized = null;
                players_randomized.Add(null);
            }
            //original  var randomizer = new Randomizer(configuration.GameplaySettings, seed);

            for (int player = 0; player < player_count; player++)
            {

                if (string.IsNullOrWhiteSpace(configuration.OutputSettings.InputPatchFilename))
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Building player Randomized for player: " + player.ToString());
                        System.Diagnostics.Debug.WriteLine("Size of players_randomized[] = " + players_randomized.Count);
                        players_randomized[player] = players_randomizer[player].Randomize(progressReporter);
                        System.Diagnostics.Debug.WriteLine("Survived the [player].Randomize(progressReporter) player: " + players_randomized.Count);
                        //original randomized = randomizer.Randomize(progressReporter);
                        //here was original spoilerlog output
                    }
                    catch (RandomizationException ex)
                    {
                        string nl = Environment.NewLine;
                        return $"Error randomizing logic: {ex.Message}{nl}{nl}Please try a different seed";
                    }
                    catch (Exception ex)
                    {
                        return ex.Message;
                    }
                }
            }
            /****
             * 
             * BREAKING BUG IN MERGE WORLDS
             * 
             * ****/


            place_player_id_item(players_randomized);
           
            System.Diagnostics.Debug.WriteLine("Merging Worlds");
            Merge_worlds(players_randomized);                   //merges worlds


            System.Diagnostics.Debug.WriteLine("Making ALL spoils");
            make_spoils(configuration,players_randomized);      //builds spoiler log

            System.Diagnostics.Debug.WriteLine("Building rom table for multi");


            //building the rom
            //randomized result is object of randomizedresult
            //holds data on play session. itemlist is holds locations of new object
            for (int player = 0; player < player_count; player++)
            {
                System.Diagnostics.Debug.WriteLine("Building ROM for player: " + player.ToString());
                if (configuration.OutputSettings.GenerateROM || configuration.OutputSettings.OutputVC || configuration.OutputSettings.GeneratePatch)
                {
                    if (!RomUtils.ValidateROM(configuration.OutputSettings.InputROMFilename))
                    {
                        return "Cannot verify input ROM is Majora's Mask (U).";
                    }

                    Multiworld_table mtable = new Multiworld_table(players_randomized[player].ItemList);
                    var builder = new Builder(players_randomized[player], configuration.CosmeticSettings, mtable);// new Multimulti_tbl);

                    try
                    {
                        Debug.WriteLine("Attempting to build rom");
                        builder.MakeROM(configuration.OutputSettings, progressReporter);
                        Debug.WriteLine("ROM constructed");
                    }
                    catch (PatchMagicException)
                    {
                        return $"Error applying patch: Not a valid patch file";
                    }
                    catch (PatchVersionException ex)
                    {
                        return $"Error applying patch: {ex.Message}";
                    }
                    catch (IOException ex)
                    {
                        return ex.Message;
                    }
                    catch (Exception ex)
                    {
                        string nl = Environment.NewLine;
                        return $"Error building ROM: {ex.Message}{nl}{nl}Please contact the development team and provide them more information";
                    }
                }
            }
            //settings.InputPatchFilename = null;

            System.Diagnostics.Debug.WriteLine("exiting Process()");
            return null;
            //return "Generation complete!";
        }

        public static void make_spoils(Configuration configuration, List<RandomizedResult> players_randomized)
        {
            if ((configuration.OutputSettings.GenerateSpoilerLog || configuration.OutputSettings.GenerateHTMLLog)
                && configuration.GameplaySettings.LogicMode != LogicMode.Vanilla)
            {
                for (int player = 0; player < Int32.Parse(configuration.GameplaySettings.sPlayerCount); player++)
                {


                    //randomized was swaped with list counterpart also spoiler log func
                    //changed to make multiple logs
                    System.Diagnostics.Debug.WriteLine("Building spoiler log for player: " + player.ToString());
                    SpoilerUtils.CreateSpoilerLog(players_randomized[player], configuration.GameplaySettings, configuration.OutputSettings);

                }
            }

        }
        //places the player id of the world the item is in
        public static void place_player_id_item(List<RandomizedResult> players_randomized)
        {
            int pid = 0;
            foreach(RandomizedResult world in players_randomized)
            {
                foreach(ItemObject item in world.ItemList)
                {
                    item.Mulitworld_player_id = pid;
                }
                pid++;
            }
            
        }
        public static void Merge_worlds(List<RandomizedResult> worlds)
        {
            int player_count = worlds.Count();
            int min_items = worlds[0].ItemList.Count(); //all worlds same items

            System.Diagnostics.Debug.WriteLine("--Merge_worlds: player_count = " +  player_count);
            System.Diagnostics.Debug.WriteLine("--Merge_worlds: min_items = " + min_items);
            ItemList p1 = new ItemList();
            ItemList p2 = new ItemList();
            //var list_itemlists = new List<ItemList>();
            for (int item = 0; item < min_items; item++)
            {
                List<ItemObject> temp_list = new List<ItemObject>();
                for (int player = 0; player < player_count; player++)
                {
                    temp_list.Add(worlds[player].ItemList[item]);
                    //list_itemlists.Add(null);
                }
                //shuffle
                temp_list.Shuffle();

                p1.Add(temp_list[0]);
                p2.Add(temp_list[1]);
                /*
                for ( int player = 0; player < player_count; player++)
                {
                    list_itemlists[player].Add(temp_list[player]);
                }*/
            }
            worlds[0].ItemList = p1;
            worlds[1].ItemList = p2;
            /*
            for( int player = 0; player < player_count; player++)
            {
                worlds[player].ItemList = list_itemlists[player];
            }*/


            // by this point all the worlds have their items back
        }




        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

    }



    public interface IProgressReporter
    {
        void ReportProgress(int percentProgress, string message);
    }
}
