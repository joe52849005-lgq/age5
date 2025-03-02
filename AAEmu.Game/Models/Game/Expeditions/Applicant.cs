﻿using System;

using AAEmu.Commons.Network;
using AAEmu.Game.Models.StaticValues;
using MySql.Data.MySqlClient;

namespace AAEmu.Game.Models.Game.Expeditions;

public class Applicant : PacketMarshaler
{
    public FactionsEnum ExpeditionId { get; set; }
    public string Memo { get; set; }
    public uint CharacterId { get; set; }
    public string CharacterName { get; set; }
    public byte CharacterLevel { get; set; }
    public DateTime RegTime { get; set; }

    public Applicant()
    {
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ExpeditionId, CharacterId);
    }

    public override bool Equals(object obj)
    {
        if (obj is Applicant other)
        {
            return ExpeditionId == other.ExpeditionId && CharacterId == other.CharacterId;
        }
        return false;
    }

    public override PacketStream Write(PacketStream stream)
    {
        stream.Write((uint)ExpeditionId);
        stream.Write(Memo);
        return stream;
    }

    public PacketStream WriteInfo(PacketStream stream)
    {
        stream.Write(CharacterId);
        stream.Write(CharacterName);
        stream.Write(CharacterLevel);
        stream.Write((uint)ExpeditionId);
        stream.Write(Memo);
        stream.Write(RegTime);
        return stream;
    }

    public void Save(MySqlConnection connection, MySqlTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Connection = connection;
        command.Transaction = transaction;

        command.CommandText = "REPLACE INTO expedition_applicants(`expedition_id`,`character_id`,`character_name`,`character_level`,`memo`,`reg_time`)" +
                              "VALUES (@expedition_id, @character_id, @character_name, @character_level, @memo, @reg_time)";
        command.Parameters.AddWithValue("@expedition_id", (uint)ExpeditionId);
        command.Parameters.AddWithValue("@character_id", CharacterId);
        command.Parameters.AddWithValue("@character_name", CharacterName);
        command.Parameters.AddWithValue("@character_level", CharacterLevel);
        command.Parameters.AddWithValue("@memo", Memo);
        command.Parameters.AddWithValue("@reg_time", RegTime);
        command.ExecuteNonQuery();
    }
}
