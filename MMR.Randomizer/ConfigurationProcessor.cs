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

namespace MMR.Randomizer
{
    //this is the loading of multi table
    public struct Multi_tbl_entry
    {
        public Multi_tbl_entry(UInt64 address, UInt16 scene_num, UInt16 flag_num, UInt16 player_num)
        {
            addr = address;
            scene = scene_num;
            flag = flag_num;
            player = player_num;
            //asdfasdfas
            //afgawrg
            //add player to id

        }

        public UInt64 addr { get; }
        public UInt32 scene { get; }
        public UInt16 flag { get; }
        public UInt16 player { get; }

    }


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
            System.Diagnostics.Debug.WriteLine("Merging Worlds");
            Merge_worlds(players_randomized);                   //merges worlds
            System.Diagnostics.Debug.WriteLine("Making ALL spoils");
            make_spoils(configuration,players_randomized);      //builds spoiler log

            System.Diagnostics.Debug.WriteLine("Building rom table for multi");
            //this needs to be written table empty if 1 player
            Multi_tbl_entry[] multi_tbl = null;
            if (player_count > 1)
                multi_tbl = build_multi_tbl(players_randomized);  //creates table entry to place in rom



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

                    var builder = new Builder(players_randomized[player], configuration.CosmeticSettings, multi_tbl);

                    try
                    {
                        builder.MakeROM(configuration.OutputSettings, progressReporter);
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
        public static Multi_tbl_entry[] build_multi_tbl(List<RandomizedResult> players_randomized)
        {
            //building the tbl for multi
            int player_count = players_randomized.Count();
            for (int player = 0; player < player_count; player++)
            {
                //place all items into list
                

            }
            return (new List<Multi_tbl_entry>()).ToArray();

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

        public static void Merge_worlds(List<RandomizedResult> worlds)
        {
            int player_count = worlds.Count();
            int min_items = worlds[0].ItemList.Count();

            //find min
            foreach ( RandomizedResult i in worlds )
            {
                min_items = (i.ItemList.Count < min_items) ? i.ItemList.Count : min_items;
                //players_item_list.Add(i.ItemList);

            }

            ItemObject[][] world_items = new ItemObject[player_count][];
            for( int player = 0; player < player_count; player++ )
            {
                world_items[player] = worlds[player].ItemList.ToArray();

            }

            //shuffle
            Shuffle(new Random(), world_items, min_items);

            for (int player = 0; player < player_count; player++)
            {
                ItemList temp = new ItemList();
                int length = world_items[player].Length;
                for(int i = 0; i < length; i++ )
                {
                    temp.Add(world_items[0][0]);
                    
                }
                worlds[player].ItemList = temp;

            }
            // by this point all the worlds have their items back
        }

        public static void Shuffle<T>(Random random, T[][] array, int lengthRow)
        {

            for (int i = array.Length - 1; i > 0; i--)
            {
                int i0 = i / lengthRow;
                int i1 = i % lengthRow;

                int j = random.Next(i + 1);
                int j0 = j / lengthRow;
                int j1 = j % lengthRow;

                T temp = array[i0][i1];
                array[i0][i1] = array[j0][j1];
                array[j0][j1] = temp;
            }
        }
    }


    public interface IProgressReporter
    {
        void ReportProgress(int percentProgress, string message);
    }
}
