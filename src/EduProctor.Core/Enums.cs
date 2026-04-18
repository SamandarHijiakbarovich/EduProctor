using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Core;


public enum UserRole
{
    SuperAdmin = 1,
    Admin = 2,
    Student = 3
}

public enum UserStatus
{
    Active = 1,
    Blocked = 2,
    Inactive = 3
}

public enum OrganizationStatus
{
    Pending = 1,
    Active = 2,
    Suspended = 3,
    Disabled = 4
}

public enum TestType
{
    MCQ = 1,
    Essay = 2,
    FillIn = 3,
    Mixed = 4
}

public enum TestStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3,
    Closed = 4
}

public enum QuestionType
{
    MCQ = 1,
    Essay = 2,
    FillIn = 3
}

public enum ExamSessionStatus
{
    Active = 1,
    Paused = 2,
    Completed = 3,
    Terminated = 4
}

public enum ProctoringEventType
{
    HeadTurn = 1,
    GazeAway = 2,
    FaceLost = 3,
    TabSwitch = 4,
    WindowBlur = 5,
    MicrophoneSpeech = 6,
    MicrophoneWhisper = 7,
    CopyPaste = 8,
    ScreenshotAttempt = 9
}

public enum ProctoringLevel
{
    Info = 1,
    Warning = 2,
    Danger = 3,
    Critical = 4
}
