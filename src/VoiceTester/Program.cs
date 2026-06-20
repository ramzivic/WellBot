using System;
using Windows.Media.SpeechSynthesis;

var voices = SpeechSynthesizer.AllVoices;
foreach (var v in voices)
{
    Console.WriteLine($"Name: {v.DisplayName}, Lang: {v.Language}, Gender: {v.Gender}, Id: {v.Id}");
}
