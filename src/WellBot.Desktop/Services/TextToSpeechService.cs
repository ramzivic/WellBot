using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Media.Playback;
using Windows.Media.Core;

namespace WellBot.Desktop.Services;

/// <summary>
/// Service de synthèse vocale (TTS) utilisant Windows.Media.SpeechSynthesis (WinRT).
/// Fournit des voix beaucoup plus naturelles et fluides que SAPI5.
/// </summary>
public interface ITextToSpeechService
{
    /// <summary>
    /// Lit un texte à voix haute de manière asynchrone.
    /// </summary>
    /// <param name="text">Le texte à lire.</param>
    /// <param name="language">Le code langue (fr, en, ar).</param>
    /// <param name="isFemale">True pour sélectionner une voix féminine, false pour masculine.</param>
    void SpeakAsync(string text, string language, bool isFemale = false);
    
    /// <summary>
    /// Arrête la lecture en cours.
    /// </summary>
    void Stop();
}

public class TextToSpeechService : ITextToSpeechService
{
    private SpeechSynthesizer? _synthesizer;
    private MediaPlayer? _mediaPlayer;

    public TextToSpeechService()
    {
        try
        {
            _synthesizer = new SpeechSynthesizer();
            // Légère diminution de la vitesse pour plus de douceur
            _synthesizer.Options.SpeakingRate = 0.9;
            _mediaPlayer = new MediaPlayer();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS Init Error: {ex}");
        }
    }

    public async void SpeakAsync(string text, string language, bool isFemale = false)
    {
        if (string.IsNullOrWhiteSpace(text) || _synthesizer == null || _mediaPlayer == null) return;

        try
        {
            var voices = SpeechSynthesizer.AllVoices;
            
            // Mapper le code langue vers un préfixe compatible
            string langPrefix = language switch
            {
                "fr" => "fr-",
                "en" => "en-",
                "ar" => "ar-",
                _ => "fr-"
            };

            var targetGender = isFemale ? VoiceGender.Female : VoiceGender.Male;

            // Liste de noms de voix préférées (les voix modernes au lieu des vieilles voix SAPI)
            string[] preferredVoices = { "julie", "paul", "mark", "hazel", "susan", "hoda", "naayf" };

            // Chercher d'abord une voix préférée correspondant à la langue et au genre
            var selectedVoice = voices.FirstOrDefault(v => v.Language.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase) && v.Gender == targetGender && preferredVoices.Any(p => v.DisplayName.ToLower().Contains(p)))
                             ?? voices.FirstOrDefault(v => v.Language.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase) && v.Gender == targetGender)
                             ?? voices.FirstOrDefault(v => v.Language.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase))
                             ?? voices.FirstOrDefault(v => v.Gender == targetGender)
                             ?? SpeechSynthesizer.DefaultVoice;

            _synthesizer.Voice = selectedVoice;

            // Générer le flux audio
            SpeechSynthesisStream stream = await _synthesizer.SynthesizeTextToStreamAsync(text);

            // Jouer le flux
            _mediaPlayer.Source = MediaSource.CreateFromStream(stream, stream.ContentType);
            _mediaPlayer.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS Synthesis Error: {ex}");
        }
    }

    public void Stop()
    {
        try
        {
            if (_mediaPlayer != null && _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                _mediaPlayer.Pause();
            }
        }
        catch { }
    }
}
