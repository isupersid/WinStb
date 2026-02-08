using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Storage;
using WinStb.Models;

namespace WinStb.Services
{
    public class ProfileService
    {
        private const string ProfilesFileName = "profiles.json";
        private const string CurrentProfileFileName = "current_profile.json";

        public async Task<List<Profile>> GetProfilesAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(ProfilesFileName) as StorageFile;
                if (file == null)
                    return new List<Profile>();

                var json = await FileIO.ReadTextAsync(file);
                return JsonConvert.DeserializeObject<List<Profile>>(json) ?? new List<Profile>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profiles: {ex.Message}");
                return new List<Profile>();
            }
        }

        public async Task SaveProfilesAsync(List<Profile> profiles)
        {
            try
            {
                var json = JsonConvert.SerializeObject(profiles, Formatting.Indented);
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    ProfilesFileName,
                    CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profiles: {ex.Message}");
            }
        }

        public async Task<Profile> AddProfileAsync(Profile profile)
        {
            var profiles = await GetProfilesAsync();
            profiles.Add(profile);
            await SaveProfilesAsync(profiles);
            return profile;
        }

        public async Task<bool> UpdateProfileAsync(Profile profile)
        {
            var profiles = await GetProfilesAsync();
            var existingProfile = profiles.FirstOrDefault(p => p.Id == profile.Id);

            if (existingProfile == null)
                return false;

            profiles.Remove(existingProfile);
            profiles.Add(profile);
            await SaveProfilesAsync(profiles);
            return true;
        }

        public async Task<bool> DeleteProfileAsync(string profileId)
        {
            var profiles = await GetProfilesAsync();
            var profile = profiles.FirstOrDefault(p => p.Id == profileId);

            if (profile == null)
                return false;

            profiles.Remove(profile);
            await SaveProfilesAsync(profiles);
            return true;
        }

        public async Task SetCurrentProfileAsync(Profile profile)
        {
            try
            {
                profile.LastUsedDate = DateTime.Now;
                await UpdateProfileAsync(profile);

                var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    CurrentProfileFileName,
                    CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting current profile: {ex.Message}");
            }
        }

        public async Task<Profile> GetCurrentProfileAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(CurrentProfileFileName) as StorageFile;
                if (file == null)
                    return null;

                var json = await FileIO.ReadTextAsync(file);
                return JsonConvert.DeserializeObject<Profile>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading current profile: {ex.Message}");
                return null;
            }
        }
    }
}
