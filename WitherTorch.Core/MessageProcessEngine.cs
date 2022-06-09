using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WitherTorch.Core
{
    public abstract class MessageProcessEngine
    {
        private static MessageProcessEngine engine;
        public static void Initialize()
        {
            if (engine == null)
                engine = new DefaultMessageProcessEngine();
        }
#if NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool TryProcessMessage(in string message, bool noStyling, out ProcessedMessage result)
        {
            bool returned = engine.TryProcessMessage_(message, noStyling, out ProcessedMessage _result);
            if (returned)
                result = _result;
            else result = null;
            return returned;
        }
        protected abstract bool TryProcessMessage_(in string message, bool noStyling, out ProcessedMessage result);
        #region Default Implementation
        private class DefaultMessageProcessEngine : MessageProcessEngine
        {
#if NET5_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            private unsafe void GetNoStylingMessage(char* charPointer, char** charPointerEndPtr)
            {
                char* charPointerEnd = *charPointerEndPtr;
                char* movablePointer = charPointer;
                char* sizingPointer = charPointer;
                int stylingState = 0;
                while (movablePointer < charPointerEnd)
                {
                    char c = *movablePointer;
                    switch (stylingState)
                    {
                        case 0: //State 0: Not in styling
                            if (c == '\u001b')
                            {
                                stylingState = 1;
                            }
                            sizingPointer++;
                            break;
                        case 1: //State 1: in Styling Header(Find left bracket)
                            if (c == '[')
                            {
                                stylingState = 2;
                                *(movablePointer - 1) = '\0';
                                *movablePointer = '\0';
                                sizingPointer--;
                            }
                            else
                            {
                                stylingState = 0;
                                sizingPointer++;
                            }
                            break;
                        case 2: //State 2: in Styling (Ignore everything but 'm')
                            if (c == 'm')
                            {
                                stylingState = 0;
                            }
                            *movablePointer = '\0';
                            break;
                    }
                    movablePointer++;
                }
                movablePointer = charPointer;
                if (sizingPointer != charPointer)
                {
                    sizingPointer++;
                    char* movablePointer2 = charPointer;
                    while (movablePointer < charPointerEnd && movablePointer2 < sizingPointer)
                    {
                        if (*movablePointer != '\0')
                        {
                            if (movablePointer2 != movablePointer)
                            {
                                *movablePointer2 = *movablePointer;
                                *movablePointer = '\0';
                            }
                            movablePointer2++;
                        }
                        movablePointer++;
                    }
                    *charPointerEndPtr = sizingPointer;
                }
            }

#if NET5_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            protected override unsafe bool TryProcessMessage_(in string message, bool noStyling, out ProcessedMessage result)
            {
                try
                {
                    fixed (char* charPointer = message)
                    {
                        int length = message.Length;
                        char* moveablePointer = charPointer;
                        char* charPointerEnd = charPointer + length;
                        if (noStyling)
                            GetNoStylingMessage(charPointer, &charPointerEnd);
                        bool successed = true;
                        bool hasContent = true;
                        ProcessedMessage.MessageTime time = ProcessedMessage.MessageTime.Empty;
                        string extraString = null, content = null;
                        int state = 0;
                        while (successed && hasContent)
                        {
                            switch (state)
                            {
                                case 0:
                                    {
                                        int bracketCount = 0;
                                        int numberTime = 0;
                                        bool fulfilled = false;
                                        fixed (sbyte* timeArr = new sbyte[3])
                                        {
                                            sbyte* movableTimePointer = timeArr;
                                            sbyte* timeArrEnd = timeArr + 2;
                                            while (moveablePointer < charPointerEnd)
                                            {
                                                char rollChar = *moveablePointer;
                                                switch (rollChar)
                                                {
                                                    case '\0':
                                                        successed = false;
                                                        break;
                                                    case '[':
                                                        bracketCount++;
                                                        goto default;
                                                    case ']':
                                                        bracketCount--;
                                                        goto default;
                                                    case ':':
                                                        if (!fulfilled)
                                                        {
                                                            if (movableTimePointer >= timeArrEnd && numberTime >= 2)
                                                            {
                                                                fulfilled = true;
                                                            }
                                                            else
                                                            {
                                                                movableTimePointer++;
                                                                numberTime = 0;
                                                            }
                                                        }
                                                        break;
                                                    default:
                                                        if (!fulfilled)
                                                        {
                                                            if (movableTimePointer >= timeArrEnd && numberTime >= 2)
                                                            {
                                                                fulfilled = true;
                                                            }
                                                            else
                                                            {
                                                                if (rollChar >= '0' && rollChar <= '9')
                                                                {
                                                                    if (numberTime < 2)
                                                                    {
                                                                        switch (numberTime)
                                                                        {
                                                                            case 0:
                                                                                *movableTimePointer = (sbyte)(rollChar - '0');
                                                                                break;
                                                                            case 1:
                                                                                *movableTimePointer = (sbyte)(*movableTimePointer * 10 + rollChar - '0');
                                                                                break;
                                                                        }
                                                                        numberTime++;
                                                                    }
                                                                    else
                                                                    {
                                                                        movableTimePointer = timeArr;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    numberTime = 0;
                                                                    movableTimePointer = timeArr;
                                                                }
                                                            }
                                                        }
                                                        break;
                                                }
                                                if (!successed || (fulfilled && bracketCount <= 0))
                                                {
                                                    break;
                                                }
                                                moveablePointer++;
                                            }
                                            if (successed)
                                            {
                                                if (fulfilled)
                                                {
                                                    time.Hour = *timeArr;
                                                    time.Minute = *(timeArr + 1);
                                                    time.Second = *timeArrEnd;
                                                    state++;
                                                }
                                                else
                                                {
                                                    successed = false;
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case 1:
                                    {
                                        if (*moveablePointer == ']') moveablePointer++;
                                        char* scanStart = moveablePointer;
                                        int bracketCount = 0;
                                        while (moveablePointer < charPointerEnd)
                                        {
                                            bool jump = false;
                                            char c = *moveablePointer;
                                            char* nextPointer = moveablePointer + 1;
                                            char nextChar;
                                            if (nextPointer < charPointerEnd)
                                            {
                                                nextChar = *nextPointer;
                                            }
                                            else
                                            {
                                                nextChar = '\0';
                                            }
                                            switch (c)
                                            {
                                                case ':':
                                                    jump = true;
                                                    break;
                                                case ' ':
                                                    if (nextChar == '[')
                                                    {
                                                        moveablePointer += 2;
                                                        bracketCount++;
                                                        continue;
                                                    }
                                                    break;
                                                case '[':
                                                    bracketCount++;
                                                    break;
                                                case ']':
                                                    if (nextChar == '[')
                                                    {
                                                        moveablePointer += 2;
                                                        continue;
                                                    }
                                                    else
                                                    {
                                                        bracketCount--;
                                                        if (nextChar == ':')
                                                        {
                                                            moveablePointer++;
                                                        }
                                                    }
                                                    break;
                                            }
                                            if (jump || bracketCount <= 0)
                                                break;
                                            moveablePointer++;
                                        }
                                        while (*++moveablePointer == ' ' && moveablePointer < charPointerEnd)
                                        {
                                        }
                                        if (moveablePointer >= charPointerEnd)
                                        {
                                            hasContent = false;
                                        }
                                        else
                                        {
                                            state++;
                                        }
                                        extraString = new string(scanStart, 0, (int)(moveablePointer - scanStart));
                                    }
                                    break;
                                case 2:
                                    {
                                        while (*moveablePointer == ' ')
                                        {
                                            moveablePointer++;
                                        }
                                        content = new string(moveablePointer, 0, (int)(charPointerEnd - moveablePointer));
                                        state++;
                                    }
                                    break;
                                case 3:
                                    {

                                    }
                                    break;
                            }
                            if (state > 2)
                            {
                                break;
                            }
                        }
                        if (successed)
                        {
                            result = new ProcessedMessage(time, content, extraString);
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                }
                result = null;
                return false;
            }
        }
        #endregion

        public class ProcessedMessage
        {
            public MessageTime Time { get; set; }
            public string Message { get; set; }
            public string Extra { get; set; }

            public ProcessedMessage(MessageTime time, string message, string extra = null)
            {
                Time = time;
                Message = message;
                Extra = extra ?? string.Empty;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MessageTime
            {
                public sbyte Hour;
                public sbyte Minute;
                public sbyte Second;

                public static MessageTime Empty = new MessageTime() { Hour = -1, Minute = -1, Second = -1 };

                public bool IsEmpty => Hour == -1;

                public MessageTime(in DateTime time)
                {
                    Hour = (sbyte)time.Hour;
                    Minute = (sbyte)time.Minute;
                    Second = (sbyte)time.Second;
                }
            }
        }

    }
}
