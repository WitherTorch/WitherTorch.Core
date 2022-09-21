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
        public static bool TryProcessMessage(string message, out ProcessedMessage result)
        {
            MessageProcessEngine engine = MessageProcessEngine.engine;
            if (engine == null)
            {
                engine = MessageProcessEngine.engine = new DefaultMessageProcessEngine();
            }
            if (engine.TryProcessMessageInternal(message, out result))
                return true;
            else
                return false;
        }

#if NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool TryProcessMessage(char[] message, out ProcessedMessage result)
        {
            MessageProcessEngine engine = MessageProcessEngine.engine;
            if (engine == null)
            {
                engine = MessageProcessEngine.engine = new DefaultMessageProcessEngine();
            }
            if (engine.TryProcessMessageInternal(message, out result))
                return true;
            else
                return false;
        }
        protected abstract bool TryProcessMessageInternal(string message, out ProcessedMessage result);
        protected abstract bool TryProcessMessageInternal(char[] message, out ProcessedMessage result);

        #region Default Implementation
        private class DefaultMessageProcessEngine : MessageProcessEngine
        {

#if NET5_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            private unsafe void GetNoStylingMessage(char* charPointer, ref char* charPointerEnd)
            {
                if (charPointer < charPointerEnd)
                {
                    char* iteratorPointer = charPointer;
                    char* limitPointer = charPointer;
                    int stylingState = 0;
                    char iteratingChar;
                    do
                    {
                        iteratingChar = *iteratorPointer;
                        switch (stylingState)
                        {
                            case 0: //State 0: Not in styling
                                if (iteratingChar == '\u001b')
                                {
                                    stylingState = 1;
                                    *iteratorPointer = default;
                                }
                                else
                                {
                                    limitPointer++;
                                }
                                break;
                            case 1: //State 1: in Styling Header(Find left bracket)
                                if (iteratingChar == '[')
                                {
                                    stylingState = 2;
                                }
                                else
                                {
                                    stylingState = 0;
                                }
                                *iteratorPointer = default;
                                break;
                            case 2: //State 2: in Styling (Ignore everything but 0x40–0x7E)
                                if (iteratingChar >= '\u0040' && iteratingChar <= '\u007E')
                                {
                                    stylingState = 0;
                                }
                                *iteratorPointer = default;
                                break;
                        }
                    } while (++iteratorPointer < charPointerEnd);
                    if (++limitPointer < charPointerEnd && charPointer < limitPointer)
                    {
                        iteratorPointer = charPointer;
                        char* iteratorPointer2 = charPointer;
                        do
                        {
                            iteratingChar = *iteratorPointer;
                            if (iteratingChar != default)
                            {
                                if (iteratorPointer != iteratorPointer2)
                                {
                                    (*iteratorPointer, *iteratorPointer2) = (default, iteratingChar);
                                }
                                iteratorPointer2++;
                            }
                            iteratorPointer++;
                        } while (iteratorPointer < charPointerEnd && iteratorPointer2 < limitPointer);
                        charPointerEnd = limitPointer;
                    }
                }
            }

#if NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            private static bool StylingCheck(char c, ref int stylingState)
            {
                if (stylingState == 0)
                {
                    if (c == '\u001b')
                    {
                        stylingState = 1;
                        return false;
                    }
                    return true;
                }
                else if (stylingState == 1)
                {
                    if (c == '[')
                        stylingState++;
                    else
                        stylingState--;
                    return false;
                }
                else
                {
                    if (c >= '\u0040' && c <= '\u007E')
                    {
                        stylingState = 0;
                    }
                    return false;
                }
            }

#if NET5_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            protected unsafe override bool TryProcessMessageInternal(string message, out ProcessedMessage result)
            {
                fixed (char* charPointer = message)
                {
                    return TryProcessMessageInternal(charPointer, message.Length, out result);
                }
            }

#if NET5_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            protected unsafe override bool TryProcessMessageInternal(char[] message, out ProcessedMessage result)
            {
                fixed (char* charPointer = message)
                {
                    return TryProcessMessageInternal(charPointer, message.Length, out result);
                }
            }

#if NET5_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#elif NET472
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            protected unsafe bool TryProcessMessageInternal(char* charPointer, int length, out ProcessedMessage result)
            {
                char* charPointerEnd = charPointer + length;
                char* iteratorPointer = charPointer;
                char iteratingChar;
                bool successed = true;
                int state = 0;
                ProcessedMessage.MessageTime time = ProcessedMessage.MessageTime.Empty;
                string extraString = null, content = null;
                do
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
                                    while (iteratorPointer < charPointerEnd)
                                    {
                                        iteratingChar = *iteratorPointer;
                                        if (StylingCheck(iteratingChar, ref stylingState))
                                        {
                                            switch (iteratingChar)
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
                                                            if (iteratingChar >= '0' && iteratingChar <= '9')
                                                            {
                                                                if (numberTime < 2)
                                                                {
                                                                    switch (numberTime)
                                                                    {
                                                                        case 0:
                                                                            *movableTimePointer = unchecked((sbyte)(iteratingChar - '0'));
                                                                            break;
                                                                        case 1:
                                                                            *movableTimePointer = unchecked((sbyte)(*movableTimePointer * 10 + iteratingChar - '0'));
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
                                        }
                                        iteratorPointer++;
                                    }
                                    if (successed)
                                    {
                                        if (fulfilled)
                                        {
                                            time = new ProcessedMessage.MessageTime(*timeArr, *(timeArr + 1), *timeArrEnd);
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
                                if (*iteratorPointer == ']') iteratorPointer++;
                                char* scanStart = iteratorPointer;
                                int bracketCount = 0, stylingState = 0;
                                char nextChar;
                                while (iteratorPointer < charPointerEnd)
                                {
                                    iteratingChar = *iteratorPointer;
                                    if (StylingCheck(iteratingChar, ref stylingState))
                                    {
                                        if (++iteratorPointer < charPointerEnd)
                                        {
                                            nextChar = *iteratorPointer;
                                        }
                                        else
                                        {
                                            nextChar = '\0';
                                        }
                                        switch (iteratingChar)
                                        {
                                            case ':':
                                                bracketCount = -1;
                                                break;
                                            case ' ':
                                                if (nextChar == '[')
                                                {
                                                    iteratorPointer += 2;
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
                                                    iteratorPointer += 2;
                                                    continue;
                                                }
                                                else
                                                {
                                                    bracketCount--;
                                                    if (nextChar == ':')
                                                    {
                                                        iteratorPointer++;
                                                    }
                                                }
                                                break;
                                        }
                                        if (bracketCount <= 0)
                                            break;
                                    }
                                    else
                                    {
                                        iteratorPointer++;
                                    }
                                }
                                while (!StylingCheck(*++iteratorPointer, ref stylingState) || (*iteratorPointer == ' ' && iteratorPointer < charPointerEnd))
                                {
                                }
                                if (iteratorPointer >= charPointerEnd)
                                {
                                    successed = false;
                                }
                                else
                                {
                                    state++;
                                }
                                char* scanEnd = iteratorPointer;
                                GetNoStylingMessage(scanStart, ref scanEnd);
                                extraString = new string(scanStart, 0, (int)(scanEnd - scanStart));
                            }
                            break;
                        case 2:
                            {
                                int stylingState = 0;
                                while (*iteratorPointer == ' ' || !StylingCheck(*iteratorPointer, ref stylingState))
                                {
                                    iteratorPointer++;
                                }
                                GetNoStylingMessage(iteratorPointer, ref charPointerEnd);
                                content = new string(iteratorPointer, 0, unchecked((int)(charPointerEnd - iteratorPointer)));
                                state++;
                            }
                            break;
                    }
                } while (successed && state <= 2);
                if (successed)
                {
                    result = new ProcessedMessage(time, content, extraString);
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
        }
        #endregion

        public class ProcessedMessage
        {
            public MessageTime Time { get; }
            public string Message { get; }
            public string Extra { get; }

            public ProcessedMessage(MessageTime time, string message, string extra)
            {
                Time = time;
                Message = message;
                Extra = extra;
            }

            [StructLayout(LayoutKind.Sequential)]
            public readonly struct MessageTime
            {
                public readonly sbyte Hour;
                public readonly sbyte Minute;
                public readonly sbyte Second;

                public static MessageTime Empty = new MessageTime(-1);

                public bool IsEmpty => Hour == -1;

                public MessageTime(DateTime time)
                {
                    unchecked
                    {
                        Hour = (sbyte)time.Hour;
                        Minute = (sbyte)time.Minute;
                        Second = (sbyte)time.Second;
                    }
                }

                private MessageTime(sbyte hour)
                {
                    Hour = hour;
                    Minute = 0;
                    Second = 0;
                }

                public MessageTime(sbyte hour, sbyte minute, sbyte second)
                {
                    Hour = hour;
                    Minute = minute;
                    Second = second;
                }
            }
        }

    }
}
