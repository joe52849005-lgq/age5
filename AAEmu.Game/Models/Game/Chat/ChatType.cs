namespace AAEmu.Game.Models.Game.Chat;

public enum ChatType : short
{
    WhisperReply = -5, // Whispered = -4, Ответ шепотом
    Whispered = -4,
    Whisper = -3,      // Whisper = -3,   Шепот
    System = -2,
    Notice = -1,
    General = 0,       // White = 0,      Общие
    Shout = 1,         // Shout = 1,      Крик
    Transactions = 2,  // Trade = 2,      Транзакции
    FindAParty = 3,    // GroupFind = 3,  Найти вечеринку
    Parties = 4,       // Party = 4,      Вечеринка
    Raid = 5,          // Raid = 5,       Рейд
    Forces = 6,        // Region = 6,     Силы
    Expeditions = 7,   // Clan = 7,       Экспедиции
    System2 = 8,
    Family = 9,        // Family = 9,      Семья
    Command = 10,      // RaidLeader = 10, Командировка
    Trial = 11,        // Judge = 11,      Испытание
    Unkn12 = 12,
    Play = 13,         //                  Играть
    Races = 14,        // Ally = 14,       Расы
    LargeTrumpet = 15, // User = 15        Большой горн
    SmallBugle = 16,   //                  Маленький горн
    Teams = 17         //                  Команда
}
