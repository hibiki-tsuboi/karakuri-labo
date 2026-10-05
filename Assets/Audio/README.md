# Audio

## Clockwork Afternoon

Original instrumental background music composed for KarakuriLabo: 96 BPM, 4/4,
32 bars / 80 seconds. Soft keys, wooden mallets, warm bass and a quiet wood pulse
match the toy-workshop art direction. Four phrases vary the melody and leave space
for thinking. There are no vocals, downloaded recordings or third-party samples.

`Assets/Editor/AtelierMusicComposer.cs` contains the score and instrument synthesis.
Use **Karakuri Labo > Audio > Regenerate Original Music**, then **Set Up Campaign**
to regenerate the stereo 44.1 kHz PCM master and apply the import settings and
scene references. Keep the WAV's `.meta` file. Tails and room reflections wrap into
the beginning of the loop; the master peaks at 0.7 (no clipping).

The player streams the Vorbis-compressed clip with source volume 0.24 and a
0.6-second fade-in. One persistent music source retains its position across NEXT.
PLAY, CLEAR and RESET leave the music running. SOUND switches both music and
the clear chime; unmuting continues the tune without replaying the chime.
Backgrounding the app pauses music and returning resumes it. Sound preferences
last for the current play session and reset on a fresh app launch.

## ClearChime

The short clear jingle is synthesized by `PhaseSevenSceneSetup.cs`. It remains a
separate, non-looping 2D source at volume 0.35 so it can play over the music.
