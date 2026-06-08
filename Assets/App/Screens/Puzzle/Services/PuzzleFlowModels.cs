using System;
using System.Collections.Generic;
using System.IO;

namespace App.Screens.Puzzle.Services
{
    public sealed class PuzzleListState
    {
        public PuzzleListState()
        {
            DisplayNames = new List<string>();
        }

        public List<string> DisplayNames { get; private set; }
    }

    public sealed class PuzzleSelectionResult
    {
        public string SelectedName { get; set; }

        public bool ShouldLaunch { get; set; }

        public string PuzzlePath { get; set; }
    }

    public sealed class PuzzleCloseActions
    {
        public bool ExitOnReturn { get; set; }

        public Action ShowMenu { get; set; }

        public Action ExitApplication { get; set; }
    }

    public sealed class PuzzleFlowCallbacks
    {
        public Func<FileInfo[]> GetPuzzleFiles { get; set; }
    }
}
