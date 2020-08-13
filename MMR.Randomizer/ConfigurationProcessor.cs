using MMR.Randomizer.Models;
using MMR.Randomizer.Models.Settings;
using MMR.Randomizer.Utils;
using MMR.Randomizer.Constants;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MMR.Randomizer
{
    //this is the loading of multi table
    public struct Multi_tbl_entry
    {
        public Multi_tbl_entry(UInt64 address, UInt16 scene_num, UInt32 flag_num)
        {
            addr = address;
            scene = scene_num;
            flag = flag_num;
            //asdfasdfas
                //afgawrg
                //add player to id

        }

        public UInt64 addr { get; }
        public UInt32 scene { get; }
        public UInt32 flag { get; }

    }


    public static class ConfigurationProcessor
    {
        public static string Process(Configuration configuration, int seed, IProgressReporter progressReporter)
        {
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
            for (int player = 0; player < Int32.Parse(configuration.GameplaySettings.sPlayerCount); player++)
            {
                System.Diagnostics.Debug.WriteLine("Building player Randomizer for player: " + player.ToString());
                players_randomizer.Add(new Randomizer(configuration.GameplaySettings, seed+player));
            }
            //original  var randomizer = new Randomizer(configuration.GameplaySettings, seed);



            /********
             * loop over creating worlds objects
             * 
             * 
             *************/

            //RandomizedResult randomized is the FINAL ranodomized object
            List<RandomizedResult> players_randomized = new List<RandomizedResult>();
            System.Diagnostics.Debug.WriteLine("Player count before null arr: " + configuration.GameplaySettings.sPlayerCount);
            for (int player = 0; player < Int32.Parse(configuration.GameplaySettings.sPlayerCount); player++)
            {
                System.Diagnostics.Debug.WriteLine("Appending null to players_randomized list for player: " + player.ToString());
                players_randomized.Add(null);
            }
            //original RandomizedResult randomized = null;


            for (int player = 0; player < Int32.Parse(configuration.GameplaySettings.sPlayerCount); player++)
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

                        /**************************
                         * 
                         * randomized is the object containing playthrough data
                         * can randomizer.Randomize() be called multiple times then swap adjacent array elems?
                         * **************************/
                        if ((configuration.OutputSettings.GenerateSpoilerLog || configuration.OutputSettings.GenerateHTMLLog)
                            && configuration.GameplaySettings.LogicMode != LogicMode.Vanilla)
                        {
                            //randomized was swaped with list counterpart also spoiler log func
                            //changed to make multiple logs
                            System.Diagnostics.Debug.WriteLine("Building spoiler log for player: " + player.ToString());
                            SpoilerUtils.CreateSpoilerLog(players_randomized[player], configuration.GameplaySettings, configuration.OutputSettings);
                        }
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


            //building the tbl for multi
            for (int player = 0; player < Int32.Parse(configuration.GameplaySettings.sPlayerCount); player++)
            {
                //Multi_tbl_entry

            }








            //building the rom
            //randomized result is object of randomizedresult
            //holds data on play session. itemlist is holds locations of new object
            for (int player = 0; player < Int32.Parse(configuration.GameplaySettings.sPlayerCount); player++)
            {
                System.Diagnostics.Debug.WriteLine("Building ROM for player: " + player.ToString());
                if (configuration.OutputSettings.GenerateROM || configuration.OutputSettings.OutputVC || configuration.OutputSettings.GeneratePatch)
                {
                    if (!RomUtils.ValidateROM(configuration.OutputSettings.InputROMFilename))
                    {
                        return "Cannot verify input ROM is Majora's Mask (U).";
                    }

                    var builder = new Builder(players_randomized[player], configuration.CosmeticSettings);

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
    }

    public interface IProgressReporter
    {
        void ReportProgress(int percentProgress, string message);
    }
}
