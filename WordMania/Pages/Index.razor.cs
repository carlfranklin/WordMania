namespace WordMania.Pages
{
    public partial class Index : ComponentBase
    {
        protected string Width = "";
        protected string CellWidth = "";
        protected string CurrentWord = "";
        protected string[]? AllWords;
        protected string KeyWidth = "";
        protected string DeadLetters = "";
        protected int CurrentRow = 0;
        protected int CurrentCol = 0;
        protected List<WordRow> Matrix = new List<WordRow>();
        protected string ErrorMessage = "";
        protected bool PlayAgain = false;

        /// <summary>
        /// Read-only property used to disable and enable the submit button
        /// </summary>
        protected bool SubmitDisabled
        {
            get
            {
                return (CurrentCol < 5);
            }
        }

        /// <summary>
        /// KeyPress is called when someone clicks or touches a keyboard key
        /// </summary>
        /// <param name="Letter"></param>
        protected void KeyPress(string Letter)
        {
            // clear the error message
            ErrorMessage = "";

            if (Letter == "DEL") // Delete? (backspace)
            {
                // If the current row and column are in range for deleting
                if (CurrentRow < 6 && CurrentCol <= 5 && CurrentCol > 0)
                {
                    // back up
                    CurrentCol--;
                    // clear the letter
                    Matrix[CurrentRow].GuessedLetters[CurrentCol] = "";
                    // change the state to "blank"
                    Matrix[CurrentRow].States[CurrentCol] = LetterState.blank;
                }
            }
            else if (Letter == "ENTER") // Enter? (return)
            {
                // if we are in a valid row at the last column...
                if (CurrentRow < 6 && CurrentCol == 5)
                {
                    // Grab the WordRow for this row
                    var wordRow = Matrix[CurrentRow];

                    // Is it a real word?
                    if (!IsRealWord(CurrentRow))
                        // nope. Show the error message
                        ErrorMessage = $"{wordRow.Guess} not a real word";
                    else // it's a real word. 
                    {
                        // DeadLetters is a string that contains all the incorrect letters that have been guessed
                        // Evaluate sets the state of each letter and returns the incorrect letters as a string
                        DeadLetters += wordRow.Evaluate();

                        // get the number of correctly positioned letters
                        int placeCount = wordRow.States.Count(x => x == LetterState.place);

                        // do we have them all correct?
                        if (placeCount == 5)
                        {
                            // Yes. The user wins
                            ErrorMessage = "You did it!";
                            // PlayAgain tells the UI to show the "Play Again" button
                            PlayAgain = true;
                            return;
                        }

                        // do we have room for a new row?
                        if (CurrentRow < 5)
                        {
                            // yes. Set CurrentRow and CurrentCol
                            CurrentRow++;
                            CurrentCol = 0;
                            // Add a new WordRow using the current word the player is trying to guess
                            Matrix.Add(new WordRow(CurrentWord));
                        }
                        else if (CurrentRow == 5)   // last row? 
                        {
                            // the player lost. Show the word
                            ErrorMessage = $"The word was {CurrentWord.ToUpper()}";
                            // PlayAgain tells the UI to show the "Play Again" button
                            PlayAgain = true;
                        }
                    }
                }
            }
            else // a letter was pressed, not DEL or ENTER
            {
                // ensure the row and column are valid
                if (CurrentRow < 6 && CurrentCol < 5)
                {
                    // the GuessedLetters property is a string array that contains the 5 letters in this guess
                    // set the letter that was pressed
                    Matrix[CurrentRow].GuessedLetters[CurrentCol] = Letter.ToUpper();
                    // set the state to "guess", meaning we're guessing this letter is accurate
                    Matrix[CurrentRow].States[CurrentCol] = LetterState.guess;
                    // move to the next column
                    CurrentCol++;
                }
            }
        }

        /// <summary>
        /// Return true if the word in this row is in our dictionary
        /// </summary>
        /// <param name="Row"></param>
        /// <returns></returns>
        protected bool IsRealWord(int Row)
        {
            // Guess is a read-only property that concatenates a string from all the letters guessed
            var guess = Matrix[Row].Guess;
            return (AllWords!.Contains(guess));
        }

        /// <summary>
        /// Used by UI. Returns a keyboard key color depending on whether it's a dead letter.
        /// </summary>
        /// <param name="Letter"></param>
        /// <returns></returns>
        protected string KeyColor(string Letter)
        {
            if (DeadLetters.IndexOf(Letter) == -1)
            {
                string lastColor = "";
                // get the state of this letter
                foreach (var wordRow in Matrix)
                {
                    var letterIndex = wordRow.Guess.IndexOf(Letter.ToLower());
                    if (letterIndex >= 0)
                    {
                        switch (wordRow.States[letterIndex])
                        {
                            case LetterState.letter:
                                lastColor = "brown";
                                break;
                            case LetterState.place:
                                lastColor = "blue";
                                break;
                        }
                    }
                }
                if (lastColor == "")
                    return "#555";
                else return lastColor;
            }
            else
                return "black";
        }

        /// <summary>
        /// Used by UI. Returns a css background-color depending on the state
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        protected string GetLetterColor(int row, int col)
        {
            if (Matrix.Count >= row + 1)
            {
                switch (Matrix[row].States[col])
                {
                    case LetterState.blank:
                    case LetterState.guess:
                        return "white";
                    case LetterState.letter:
                        return "orange";
                    case LetterState.place:
                        return "lightblue";
                    case LetterState.incorrect:
                        return "gray";
                }
            }
            return "white";
        }

        /// <summary>
        /// Used by UI to retrieve the letter given the row and column
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        protected string GetLetter(int row, int col)
        {
            // is there a WordRow at this row?
            if (Matrix.Count >= row + 1)
            {
                // if the state is NOT "blank:
                if (Matrix[row].States[col] != LetterState.blank)
                    // return the letter at this column
                    return Matrix[row].GuessedLetters[col];
                else
                    return "";
            }
            return "";
        }

        protected override async Task OnInitializedAsync()
        {
            // load the words from the text file into a string
            var wordsString = await Http.GetStringAsync("words.txt");
            // convert to string array
            AllWords = wordsString.Split("\n");
            // set up a new game
            ResetGame();
        }

        protected void ResetGame()
        {
            // initialize variables
            PlayAgain = false;
            ErrorMessage = "";
            DeadLetters = "";
            Matrix.Clear();
            CurrentRow = 0;
            CurrentCol = 0;
            // pick a new word
            CurrentWord = GetRandomWord();
            // create a new WordRow from this word
            var wordRow = new WordRow(CurrentWord);
            // add it to our list of WordRows
            Matrix.Add(wordRow);
        }

        /// <summary>
        /// Use the RandomNumberGenerator to easily pick a random word from the AllWords arrao
        /// </summary>
        /// <returns></returns>
        protected string GetRandomWord()
        {
            if (AllWords == null) return "";

            var index = RandomNumberGenerator.GetInt32(AllWords!.Length - 1);
            return AllWords[index];
        }
        
        [JSInvokable]
        public async Task Resize(WindowDimension Size)
        {
            // Ensure a good width to height ratio
            var maxWidth = Size.Height * .55;
            if (Size.Width > maxWidth)
            {
                Size.Width = Convert.ToInt32(maxWidth);
            }

            // shave off some of the page width to get the grid width
            int width = Size.Width - Convert.ToInt32((Size.Width * .2));

            // shave off less of the page width to get the keyboard width
            int keyboardWidth = Size.Width - Convert.ToInt32((Size.Width * .1));

            // The Width string is used to set the table width
            Width = width.ToString() + "px";

            // each cell is 1/5th of the grid width
            CellWidth = (width / 5).ToString() + "px";

            // each key in the keyboard is 1/11th the keyboardWidth width
            KeyWidth = (keyboardWidth / 11).ToString() + "px";

            // render
            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // get the dimensions of the screen from JavaScript, passing in this 
                // component reference so we can handle resize events
                var size = await JS.InvokeAsync<WindowDimension>("getWindowDimensions",
                     DotNetObjectReference.Create(this));
                
                await Resize(size);
            }
        }
    }

    /// <summary>
    /// WordRow encapsulates the logic and data for each a row in the grid
    /// </summary>
    public class WordRow
    {
        // The word we're trying to guess (passed in from CurrentWord)
        public string Word { get; set; } = "";

        // States for each of the 5 letters
        public LetterState[] States { get; set; }

        // Letters the player has guessed for this row
        public string[] GuessedLetters { get; set; }

        // The constructor takes the word and initializes the states
        public WordRow(string word)
        {
            this.Word = word;
            States = new LetterState[5];
            GuessedLetters = new string[5];
        }

        /// <summary>
        /// Guess is a read-only property that concatenates a string from all the letters guessed
        /// </summary>
        public string Guess
        {
            get
            {
                return ($"{GuessedLetters[0]}{GuessedLetters[1]}{GuessedLetters[2]}{GuessedLetters[3]}{GuessedLetters[4]}").ToLower();
            }
        }

        /// <summary>
        /// Evaluate sets the state of each letter and returns the incorrect letters as a string 
        /// </summary>
        /// <returns></returns>
        public string Evaluate()
        {
            string deadLetters = "";

            // leftInWord is pruned as letters are guessed.
            // we use it to determine if a given letter is valid
            string leftInWord = Word.ToUpper();

            // find exact matches
            for (int col = 0; col < 5; col++)
            {
                // the letter being guessed at this position
                var guessLetter = Guess.Substring(col, 1).ToUpper();

                // the letter in the word at this position
                var wordLetter = Word.Substring(col, 1).ToUpper();

                // are they equal?
                if (guessLetter == wordLetter)
                {
                    // yes! set the state to "place" to indicate the right letter in the right place
                    States[col] = LetterState.place;

                    // remove the letter from our search word
                    var index = leftInWord.IndexOf(guessLetter);
                    if (index != -1)
                        leftInWord = leftInWord.Remove(index, 1);
                }

            }

            // find fuzzy matches
            for (int col = 0; col < 5; col++)
            {
                // the letter being guessed at this position
                var guessLetter = Guess.Substring(col, 1).ToUpper();

                // the letter in the word at this position
                var wordLetter = Word.Substring(col, 1).ToUpper();

                // if they are NOT equal...
                if (guessLetter != wordLetter)
                {
                    // is the letter there, but in the wrong place?
                    if (leftInWord.Contains(guessLetter))
                        // yes. set the state to "letter" indicating the right letter in the wrong position
                        States[col] = LetterState.letter;
                    else
                    {
                        // no. The guessed letter is NOT in the word. 
                        // add the letter to the deadletters string, but only if the letter doesn't exist in the word
                        if (!Word.ToUpper().Contains(guessLetter))
                            deadLetters += guessLetter;
                        // set the state to "incorrect" indicating the letter is not in the word at all.
                        States[col] = LetterState.incorrect;
                    }
                }

                // remove the letter from our search word
                var index = leftInWord.IndexOf(guessLetter);
                if (index != -1)
                    leftInWord = leftInWord.Remove(index, 1);
            }

            return deadLetters;
        }
    }

    /// <summary>
    /// window size returned by JavaScript
    /// </summary>
    public class WindowDimension
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    /// <summary>
    /// Identifies the state of a letter in a row
    /// </summary>
    public enum LetterState
    {
        blank,      // not guessed or evaluated
        guess,      // being guessed
        incorrect,  // the letter does not exist in the word
        letter,     // the letter exists but is in the wrong place
        place       // the letter exists in the right place
    }
}