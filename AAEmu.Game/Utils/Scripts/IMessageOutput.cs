using System.Collections.Generic;
using System.Drawing;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Chat;

namespace AAEmu.Game.Utils.Scripts;

public interface IMessageOutput
{
    IEnumerable<string> Messages { get; }
    IEnumerable<string> ErrorMessages { get; }

    void SendDebugMessage(string message);
    void SendDebugMessage(ChatType chatType, string message, Color? color = null);
    void SendDebugMessage(ICharacter target, string message);
}
