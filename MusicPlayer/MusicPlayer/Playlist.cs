using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer
{
    class Playlist
    {
        #region Fields/Properties

        public ObservableCollection<MusicFile> MusicList { get; set; }
        public MusicFile this[int index] { get { return MusicList[index]; } }
        public int Count { get { return MusicList.Count; } }
        public string Name { get; set; }

        #endregion

        #region Constructors

        public Playlist(string name, MusicFile[] files)
        {
            Name = name;
            foreach (var item in files)
            {
                MusicList.Add(item);
            }
        }

        public Playlist(string name, MusicFile file)
        {
            Name = name;
            MusicList.Add(file);
        }

        public Playlist(string name, ObservableCollection<MusicFile> playlist)
        {
            Name = name;
            MusicList = playlist;
        }

        #endregion
    }
}
