using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WitherTorch.Core
{
    public abstract class MessageProcessEngine
    {
        private static MessageProcessEngine engine;

#if NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Initialize(MessageProcessEngine engine)
        {
            if (MessageProcessEngine.engine == null)
                MessageProcessEngine.engine = engine;
        }

#if NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Initialize()
        {
            Initialize(new DefaultMessageProcessEngine());
        }

#if NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool TryProcessMessage(string message, bool noStyling, out ProcessedMessage result)
        {
            MessageProcessEngine engine = MessageProcessEngine.engine;
            if (engine == null)
            {
                engine = MessageProcessEngine.engine = new DefaultMessageProcessEngine();
            }
            if (engine.TryProcessMessageInternal(message, noStyling, out result))
                return true;
            else
                return false;
        }

#if NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool TryProcessMessage(char[] message, bool noStyling, out ProcessedMessage result)
        {
            MessageProcessEngine engine = MessageProcessEngine.engine;
            if (engine == null)
            {
                engine = MessageProcessEngine.engine = new DefaultMessageProcessEngine();
            }
            if (engine.TryProcessMessageInternal(message, noStyling, out result))
                return true;
            else
                return false;
        }
        protected abstract bool TryProcessMessageInternal(string message, bool noStyling, out ProcessedMessage result);
        protected abstract bool TryProcessMessageInternal(char[] message, bool noStyling, out ProcessedMessage result);

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
                        case 2: //State 2: in Styling (Ignore everything but 0x40–0x7E)
                            if (c >= '\u0040' && c <= '\u007E')
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

            private static bool StylingCheck(char c, ref int stylingState)
            {
                switch (stylingState)
                {
                    case 0: //State 0: Not in styling
                        if (c == '\u001b')
                        {
                            stylingState = 1;
                        }
                        return true;
                    case 1: //State 1: in Styling Header(Find left bracket)
                        if (c == '[')
                        {
                            stylingState = 2;
                        }
                        else
                        {
                            stylingState = 0;
                        }
                        return true;
                    case 2: //State 2: in Styling (Ignore everything but 0x40–0x7E)
                        if (c >= '\u0040' && c <= '\u007E')
                        {
                            stylingState = 0;
                        }
                        return true;
                }
                return false;
            }

#if NET5_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            protected unsafe override bool TryProcessMessageInternal(string message, bool noStyling, out ProcessedMessage result)
            {
                fixed (char* charPointer = message)
                {
                    return TryProcessMessageInternal(charPointer, message.Length, noStyling, out result);
                }
            }

#if NET5_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            protected unsafe override bool TryProcessMessageInternal(char[] message, bool noStyling, out ProcessedMessage result)
            {
                fixed (char* charPointer = message)
                {
                    return TryProcessMessageInternal(charPointer, message.Length, noStyling, out result);
                }
            }

#if NET5_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            protected unsafe bool TryProcessMessageInternal(char* charPointer, int length, bool noStyling, out ProcessedMessage result)
            {
                try
                {
                    char* moveablePointer = charPointer;
                    char* charPointerEnd = charPointer + length;
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
                                        int stylingState = 0;
                                        while (moveablePointer < charPointerEnd)
                                        {
                                            char rollChar = *moveablePointer;
                                            if (StylingCheck(rollChar, ref stylingState))
                                                continue;
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
                                    int stylingState = 0;
                                    while (moveablePointer < charPointerEnd)
                                    {
                                        char c = *moveablePointer;
                                        if (StylingCheck(c, ref stylingState))
                                            continue;
                                        bool jump = false;
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
                                    while (StylingCheck(*++moveablePointer, ref stylingState) || (*moveablePointer == ' ' && moveablePointer < charPointerEnd))
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
                                    int stylingState = 0;
                                    while (*moveablePointer == ' ' || StylingCheck(*moveablePointer, ref stylingState))
                                    {
                                        moveablePointer++;
                                    }
                                    GetNoStylingMessage(moveablePointer, &charPointerEnd);
                                    content = new string(moveablePointer, 0, (int)(charPointerEnd - moveablePointer));
                                    state++;
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
                catch (Exception)
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
