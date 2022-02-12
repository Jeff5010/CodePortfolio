# CodePortfolio
This project consists of two different C#/WPF applications.

The first application runs on a tablet. The user takes a photo, and is given the chance to approve the photo, or to take a new one. 
Once the photo is approved, it is sent to the PC running the second application.

The second application runs on a PC, which will be hooked up to a display. When the PC receives a photo from the tablet, the photo will be run through
an AfterEffects process to create a video. Once the process is complete, the video will be moved to a storage folder on the PC, and the display will then
run the video, and begin waiting for the next photo from the tablet.
