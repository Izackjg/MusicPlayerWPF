using System;

namespace MusicPlayer
{
    class MusicFile
    {
        public string Title { get; set; }
        public string FilePath { get; set; }
        public TimeSpan Duration { get; set; }

        public MusicFile(string title, string filePath, TimeSpan duration)
        {
            this.Title = title;
            this.FilePath = filePath;
            this.Duration = duration;
        }      
    }
}
