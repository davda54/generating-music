## Survey

This folder contains files related to the online survey.

Songs folder contains all 104 songs used, we provide both in the original .mid format and in the rendered .mp3 format that was used in the survey. The names are related to their origin:
 - [1-24].mid are multi-instrumental human pieces
 - [25-52].mid are single-instrumental human pieces
 - [53-76].mid are multi-instrumental computer pieces
 - [77-104].mid are single-instrumental computer pieces
 
result.csv contains a table with results with columns USER_ID (unique identificator of a user), SONG_ID (number corresponding to the name of a song), GUESS (contains three possible numbers -- 0 means the respondend thought the song was created by a human, 1 means computer and 2 means that the song seemed to be familiar), RATING (rating of quality from 0 (bad) to 5 (awesome)), TIME (listening time in milliseconds) and voluntory COMMENT. 