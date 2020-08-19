using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NewTezla
{
    internal class GetAnswers
    {
        /// <summary>
        ///     The Checkbox to be shown at the end of a not selected answer when multiple choice
        /// </summary>
        private const string EmptyAnswerBox = " [ ]";

        /// <summary>
        ///     The Checkbox to be shown at the end of a selected answer when multiple choice
        /// </summary>
        private const string FilledAnswerBox = " [X]";

        /// <summary>
        ///     The cursor position after the question is written to the console
        /// </summary>
        private static int _afterQuestionCursorTop;

        /// <summary>
        ///     The string the user is searching after in the answers
        /// </summary>
        private static string _searchString;

        /// <summary>
        ///     The Dictionary containing the startingNumber of an answer
        /// </summary>
        private static readonly Dictionary<string, int> LineNumbers = new Dictionary<string, int>();

        /// <summary>
        ///     Gets the user choice from the user
        /// </summary>
        /// <param name="question">The Question/Choice you want the user to Answer/Make</param>
        /// <param name="answers">The Answers/Choices that the user can choose between</param>
        /// <returns>The answer the user selected as the int representing the position of the answer (starting from 0)</returns>
        public static int GetChoiceFromListAsInt(string question, params string[] answers)
        {
            return GetChoice(question, answers).FirstOrDefault();
        }


        /// <summary>
        ///     Gets the user choice from the user
        /// </summary>
        /// <param name="question">The Question/Choice you want the user to Answer/Make</param>
        /// <param name="answers">The Answers/Choices that the user can choose between</param>
        /// <param name="finishedSelectionChoice">The option you want the user to pick when they have selected all their answers</param>
        /// <returns>The answers the user selected as an int[] where the ints represents the positions of the answers (starting from 0)</returns>
        public static int[] GetMultipleChoiceFromListAsInts(string question, string finishedSelectionChoice, params string[] answers)
        {
            return GetChoice(question, answers, finishedSelectionChoice, true);
        }

        /// <summary>
        ///     Gets the user choice from the user
        /// </summary>
        /// <param name="question">The Question/Choice you want the user to Answer/Make</param>
        /// <param name="answers">The Answers/Choices that the user can choose between</param>
        /// <returns>The answer the user selected</returns>
        public static string GetChoiceFromListAsString(string question, params string[] answers)
        {
            int index = GetChoiceFromListAsInt(question, answers);
            return answers[index];
        }

        /// <summary>
        ///     Gets the user choice from the user
        /// </summary>
        /// <param name="question">The Question/Choice you want the user to Answer/Make</param>
        /// <param name="answers">The Answers/Choices that the user can choose between</param>
        /// <param name="finishedSelectionChoice">The option you want the user to pick when they have selected all their answers</param>
        /// <returns>The answers the user selected</returns>
        public static string[] GetMultipleChoiceFromListAsStrings(string question, string finishedSelectionChoice, params string[] answers)
        {
            int[] indexes = GetMultipleChoiceFromListAsInts(question, finishedSelectionChoice, answers);

            string[] returnAnswers = new string[indexes.Length];

            for (int i = 0; i < indexes.Length; i++)
            {
                int index = indexes[i];

                returnAnswers[i] = answers[index];
            }

            return returnAnswers;
        }

        /// <summary>
        ///     Gets the user choice from the user as an enum,
        /// </summary>
        /// <typeparam name="TEnumType">The enum type you want to use to get answers</typeparam>
        /// <param name="question">The Question/Choice you want the user to Answer/Make</param>
        /// <returns>The answer from the user as a Enum of type TEnumType</returns>
        public static TEnumType GetChoiceFromEnum<TEnumType>(string question) where TEnumType : struct, Enum
        {
            string[] enumNames = GetEnumNames<TEnumType>();

            var answer = GetChoiceFromListAsString(question, enumNames);

            if (Enum.TryParse(answer, out TEnumType returnValue))
            {
                return returnValue;
            }
            else
            {
                throw new InvalidCastException("Cannot get value from the enum");
            }
        }

        /// <summary>
        ///   Gets the user choice from the user as multiple choice from an enum,
        /// </summary>
        /// <typeparam name="TEnumType">The enum type you want to use to get answers</typeparam>
        /// <param name="question">The Question/Choice you want the user to Answer/Make</param>
        /// <param name="finishedSelectionChoice">The option you want the user to pick when they have selected all their answers</param>
        /// <returns>The answer from the user as a Enum[] of type TEnumType[]</returns>
        public static TEnumType[] GetMultipleChoiceFromEnum<TEnumType>(string question, string finishedSelectionChoice) where TEnumType : struct, Enum
        {
            string[] enumNames = GetEnumNames<TEnumType>();

            var answers = GetMultipleChoiceFromListAsStrings(question, finishedSelectionChoice, enumNames);

            TEnumType[] returnAnswers = new TEnumType[answers.Length];

            for (int i = 0; i < answers.Length; i++)
            {
                string answer = answers[i];
                if (Enum.TryParse(answer, out TEnumType returnAnswer))
                {
                    returnAnswers[i] = returnAnswer;
                }
                else
                {
                    throw new InvalidCastException("Cannot get value from the enum");
                }
            }

            return returnAnswers;

        }


        /// <summary>
        ///     Gets the user choice from the user
        /// </summary>
        /// <param name="question">The Question/Choice you want the user to Answer/Make</param>
        /// <param name="answers">The Answers/Choices that the user can choose between</param>
        /// <param name="finishedSelectionChoice">The option you want the user to pick when they have selected all their answers</param>
        /// <param name="MultipleChoice">Whether or not the selection is in multiple choice mode</param>
        /// <returns>The answers the user selected as an int[] where the ints represents the positions of the answers (starting from 0), It will always return only 1 element if not in multiple choice mode</returns>
        private static int[] GetChoice(string question, string[] answers, string finishedSelectionChoice = "", bool MultipleChoice = false)
        {
            List<int> selectedAnswersIndexes = new List<int>();

            if (MultipleChoice)
            {
                Array.Resize<string>(ref answers, answers.Length + 1);
                AddCheckBoxes(answers);
                answers[answers.Length - 1] = finishedSelectionChoice;

            }


            try
            {
                int startBufferWidth = Console.BufferWidth;
                int startWindowWidth = Console.WindowWidth;
                Console.BufferWidth = short.MaxValue - 1;
                GetLineNumbers(answers);
                Console.BufferWidth = Math.Max(Math.Min(LineNumbers.Keys.Max(i => i.Length) + 1, short.MaxValue - 1),
                    Console.WindowWidth);
                int setBufferWidth = Console.BufferWidth;
                int index = 0;
                int cursorTop = Console.CursorTop;
                ConsoleKey pressedKey = ConsoleKey.Enter;
                Console.CursorVisible = false;

                Console.WriteLine(question);
                _afterQuestionCursorTop = Console.CursorTop;

                Answers(answers, index);
                int afterAnswersCursorTop = Console.CursorTop;
                Console.CursorTop = cursorTop + 1;
                DateTime timeBetweenKeyPresses = DateTime.Now;
                do
                {
                    do
                    {
                        MakeAnswerLineNormal(index, answers);

                        index = GetNewIndexFromKeyPress(answers, index, pressedKey);
                        ClearLine(_afterQuestionCursorTop + LineNumbers[answers[index].Replace(FilledAnswerBox, EmptyAnswerBox)]);
                        PrintIndexAnswer(index, answers);
                        int beforeResizeCursorTop = Console.CursorTop;
                        pressedKey = Console.ReadKey(true).Key;
                        if (Console.BufferWidth != setBufferWidth)
                        {
                            startBufferWidth = Console.BufferWidth;
                            startWindowWidth = Console.WindowWidth;
                        }

                        Console.BufferWidth = Math.Max(Math.Min(LineNumbers.Keys.Max(i => i.Length) + 1, short.MaxValue - 1),
                            Console.WindowWidth);
                        if ((DateTime.Now - timeBetweenKeyPresses).Seconds > 1)
                        {
                            _searchString = "";
                        }

                        timeBetweenKeyPresses = DateTime.Now;

                        CheckForResize(beforeResizeCursorTop);
                    } while (pressedKey != ConsoleKey.Enter);

                    if (MultipleChoice)
                    {
                        if (selectedAnswersIndexes.Contains(index))
                        {
                            answers[index] = answers[index].Replace(FilledAnswerBox, EmptyAnswerBox);
                            selectedAnswersIndexes.Remove(index);
                        }
                        else
                        {
                            selectedAnswersIndexes.Add(index);
                            answers[index] = answers[index].Replace(EmptyAnswerBox, FilledAnswerBox);
                        }
                    }
                    else
                    {
                        selectedAnswersIndexes.Add(index);
                    }

                } while (index != answers.Length - 1 && MultipleChoice);


                Console.CursorTop = cursorTop;
                while (Console.CursorTop != afterAnswersCursorTop)
                {
                    Console.WriteLine(new string(' ', Console.BufferWidth - 1));
                }

                Console.CursorTop = cursorTop;
                Console.CursorVisible = true;
                Console.WindowWidth = startWindowWidth;
                Console.BufferWidth = startBufferWidth;
                LineNumbers.Clear();

                if (MultipleChoice && selectedAnswersIndexes.Contains(answers.Length - 1))
                {
                    selectedAnswersIndexes.Remove(answers.Length - 1);
                }


                return selectedAnswersIndexes.OrderBy(i => i).ToArray();

            }
            catch (IOException)
            {
                while (true)
                {
                    Console.WriteLine("This application have a gui interface that does not work correctly with your Terminal\nIn some terminals this application will not work at all\nFor the best experience run this application from windows terminal.\n\nPlease Enter the correct number and press enter");

                    int choice;

                    do
                    {
                        if (MultipleChoice)
                        {
                            RemoveCheckBoxes(answers);
                            Console.WriteLine("Selected Answers");
                            foreach (var selectedIndexes in selectedAnswersIndexes)
                            {
                                Console.WriteLine(answers[selectedIndexes]);
                            }
                            Console.WriteLine();
                        }
                        Console.WriteLine(question);

                        for (int index = 0; index < answers.Length; index++)
                        {
                            string answer = answers[index];
                            Console.WriteLine($"Enter {index} for: {answer}");
                        }

                        if (!int.TryParse(Console.ReadLine(), out choice) || choice > answers.Length)
                        {
                            Console.WriteLine("Invalid choice\nPress Any Key To Continue");
                            Console.ReadKey(true);
                            Console.WriteLine();
                            Console.WriteLine();
                        }
                        else
                        {
                            if (selectedAnswersIndexes.Contains(choice))
                            {
                                selectedAnswersIndexes.Remove(choice);
                            }
                            else
                            {
                                selectedAnswersIndexes.Add(choice);
                            }
                        }
                    }
                    while (choice != answers.Length - 1 && MultipleChoice);

                    if (MultipleChoice && selectedAnswersIndexes.Contains(answers.Length - 1))
                    {
                        selectedAnswersIndexes.Remove(answers.Length - 1);
                    }

                    return selectedAnswersIndexes.OrderBy(i => i).ToArray();
                }
            }
        }


        /// <summary>
        ///     Gets the lineNumber for each answer and saves it in the lineNumbers dictionary
        /// </summary>
        /// <param name="answers">The Answers/Choices that the user can choose between</param>
        private static void GetLineNumbers(params string[] answers)
        {
            int currentLineNumber = 0;
            foreach (string answer in answers)
            {
                string[] lines = answer.Split('\n');
                try
                {
                    LineNumbers.Add(answer, currentLineNumber + 1);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException("Same answer appeared multiple time.\nAnswers: " + answer + "\n", e);
                }

                foreach (string line in lines)
                {
                    currentLineNumber += line.Length / Console.BufferWidth + 1;
                }
            }
        }

        /// <summary>
        ///     Checks if the Console.CursorTop has changed do to a resize and makes sure to handle the resize so that the method
        ///     does not break;
        /// </summary>
        /// <param name="beforeResizeCursorTop">the position og the cursorTop before the resize</param>
        private static void CheckForResize(int beforeResizeCursorTop)
        {
            if (Console.CursorTop < beforeResizeCursorTop)
            {
                _afterQuestionCursorTop -= Math.Abs(Console.CursorTop - beforeResizeCursorTop);
            }
            else if (Console.CursorTop > beforeResizeCursorTop)
            {
                _afterQuestionCursorTop += Math.Abs(Console.CursorTop - beforeResizeCursorTop);
            }
        }

        /// <summary>
        ///     Gets the new selected index by checking the keypress.
        /// </summary>
        /// <param name="answers">The Answers/Choices that the user can choose between</param>
        /// <param name="index">The current index</param>
        /// <param name="pressedKey">The key that was pressed by the user</param>
        /// <returns></returns>
        private static int GetNewIndexFromKeyPress(string[] answers, int index, ConsoleKey pressedKey)
        {
            switch (pressedKey)
            {
                case ConsoleKey.UpArrow:
                    index--;
                    if (index < 0)
                    {
                        index = answers.Length - 1;
                    }

                    _searchString = "";
                    break;
                case ConsoleKey.DownArrow:
                    index++;
                    if (index >= answers.Length)
                    {
                        index = 0;
                    }

                    _searchString = "";
                    break;
                case ConsoleKey.PageUp:
                    index -= 5;
                    if (index < 0)
                    {
                        index = 0;
                    }

                    _searchString = "";
                    break;
                case ConsoleKey.PageDown:
                    index += 5;
                    if (index >= answers.Length)
                    {
                        index = answers.Length - 1;
                    }

                    _searchString = "";
                    break;
                case ConsoleKey.End:
                    index = answers.Length - 1;
                    _searchString = "";
                    break;
                case ConsoleKey.Home:
                    index = 0;
                    _searchString = "";
                    break;
                case ConsoleKey.Enter:
                    _searchString = "";
                    break;
                default:
                    string pressedKeyString = pressedKey.ToString().ToLower();
                    _searchString += pressedKeyString;
                    bool doBreak = FindMatchingAnswerIndex(answers, ref index);
                    if (doBreak)
                    {
                        break;
                    }

                    if (_searchString.Length > 1 && answers[index].StartsWith(_searchString))
                    {
                        break;
                    }

                    _searchString = pressedKeyString;
                    FindMatchingAnswerIndex(answers, ref index);
                    break;
            }

            return index;
        }

        /// <summary>
        ///     checks if any of the answers starts with the characters the user searches for
        /// </summary>
        /// <param name="answers">The Answers/Choices that the user can choose between</param>
        /// <param name="index">the current index (changes if answer is found)</param>
        /// <returns>whether an answer matching the search was found</returns>
        private static bool FindMatchingAnswerIndex(string[] answers, ref int index)
        {
            for (int i = index + 1; i < answers.Length; i++)
            {
                if (answers[i].ToLower().StartsWith(_searchString))
                {
                    index = i;
                    return true;
                }
            }

            for (int i = 0; i < index - 1; i++)
            {
                if (answers[i].ToLower().StartsWith(_searchString))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     prints the answers and highlights the selected answer
        /// </summary>
        /// <param name="answers">The Answers/Choices that the user can choose between</param>
        /// <param name="index">the current index</param>
        private static void Answers(string[] answers, int index)
        {
            for (int i = 0; i < answers.Length; i++)
            {
                if (i == index)
                {
                    PrintIndexAnswer(i, answers);
                }
                else
                {
                    Console.WriteLine(answers[i]);
                }
            }
        }

        /// <summary>
        ///     prints out the answer corresponding to the current index in the highlighted form
        /// </summary>
        /// <param name="index">the current index</param>
        /// <param name="answers">The Answers/Choices that the user can choose between</param>
        private static void PrintIndexAnswer(int index, string[] answers)
        {
            Console.SetCursorPosition(0, _afterQuestionCursorTop + LineNumbers[answers[index].Replace(FilledAnswerBox, EmptyAnswerBox)]);
            ConsoleColor foregroundColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            if (foregroundColor == ConsoleColor.Cyan)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            Console.WriteLine(answers[index]);
            Console.ForegroundColor = foregroundColor;
        }

        /// <summary>
        ///     prints out the answer corresponding to the current index in the not highlighted form
        /// </summary>
        /// <param name="index"></param>
        /// <param name="answers"></param>
        private static void MakeAnswerLineNormal(int index, string[] answers)
        {
            Console.SetCursorPosition(0, _afterQuestionCursorTop + LineNumbers[answers[index].Replace(FilledAnswerBox, EmptyAnswerBox)]);
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.CursorLeft = 0;
            Console.WriteLine(answers[index]);
        }

        /// <summary>
        ///     clears the line specified
        /// </summary>
        /// <param name="lineNumber">the line you want to be cleared</param>
        private static void ClearLine(int lineNumber)
        {
            Console.CursorTop = lineNumber;
            Console.Write("\r" + new string(' ', Console.BufferWidth - 1) + "\r");
            Console.CursorLeft = 0;
        }

        /// <summary>
        /// Adds empty checkBoxes to the end of all answers
        /// </summary>
        /// <param name="answers"></param>
        private static void AddCheckBoxes(string[] answers)
        {
            for (int i = 0; i < answers.Length; i++)
            {
                string answer = answers[i];
                answer += EmptyAnswerBox;
                answers[i] = answer;
            }
        }

        /// <summary>
        /// removes checkBoxes from the end of all answers
        /// </summary>
        /// <param name="answers"></param>
        private static void RemoveCheckBoxes(string[] answers)
        {
            for (int i = 0; i < answers.Length; i++)
            {
                string answer = answers[i];
                answer = answer.Replace(EmptyAnswerBox, "").Replace(FilledAnswerBox, "");
                answers[i] = answer;
            }
        }

        /// <summary>
        /// Gets all the values the enum can have as a string[]
        /// </summary>
        /// <typeparam name="TEnumType">The Enum type you want to get the values from</typeparam>
        /// <returns></returns>
        private static string[] GetEnumNames<TEnumType>() where TEnumType : struct, Enum
        {
            var enumNames = Enum.GetNames(typeof(TEnumType));
            if (typeof(TEnumType).BaseType != typeof(Enum))
            {
                throw new InvalidCastException();
            }

            return enumNames;
        }
    }
}