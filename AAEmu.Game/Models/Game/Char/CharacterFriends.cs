﻿using System.Collections.Generic;

using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.Id;
using AAEmu.Game.Core.Packets.G2C;

using MySql.Data.MySqlClient;

namespace AAEmu.Game.Models.Game.Char;

public class CharacterFriends
{
    public Character Owner { get; set; }
    public Dictionary<uint, FriendTemplate> FriendsIdList { get; set; } // friendId, Template
    private readonly List<uint> _removedFriends; // friendId

    public CharacterFriends(Character owner)
    {
        Owner = owner;
        FriendsIdList = new Dictionary<uint, FriendTemplate>();
        _removedFriends = new List<uint>();
    }

    public void AddFriend(string name)
    {
        var friend = FriendMananger.GetFriendInfo(name);
        if (Owner.Name == name)
        {
            Owner.SendErrorMessage(ErrorMessageType.CannotAddFriendSelf);
            return;
        }
        if (friend == null || FriendsIdList.ContainsKey(friend.CharacterId))
        {
            Owner.SendErrorMessage(ErrorMessageType.AddFriend);
            return;
        }

        var template = new FriendTemplate();
        template.Id = FriendIdManager.Instance.GetNextId();
        template.FriendId = friend.CharacterId;
        template.Owner = Owner.Id;
        FriendsIdList.Add(friend.CharacterId, template);
        FriendMananger.Instance.AddToAllFriends(template);
        Owner.SendPacket(new SCAddFriendPacket(friend, true, 0));
    }

    public void RemoveFriend(string name)
    {
        var friend = FriendMananger.GetFriendInfo(name);
        if (friend == null || !FriendsIdList.ContainsKey(friend.CharacterId))
        {
            Owner.SendErrorMessage(ErrorMessageType.DeleteFriend);
            return;
        }

        FriendMananger.Instance.RemoveFromAllFriends(FriendsIdList[friend.CharacterId].Id);
        FriendsIdList.Remove(friend.CharacterId);
        _removedFriends.Add(friend.CharacterId);
        Owner.SendPacket(new SCDeleteFriendPacket(friend.CharacterId, true, name, 0));
    }

    public void Send()
    {
        if (FriendsIdList.Count <= 0) return;

        var allFriends = FriendMananger.GetFriendInfo(new List<uint>(FriendsIdList.Keys));
        var allFriendsArray = new Friend.Friend[allFriends.Count];
        allFriends.CopyTo(allFriendsArray, 0);
        Owner.SendPacket(new SCFriendListsPacket(allFriendsArray.Length, allFriendsArray));
    }

    public void Load(MySqlConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM friends WHERE `owner` = @owner";
            command.Parameters.AddWithValue("@owner", Owner.Id);
            command.Prepare();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var template = new FriendTemplate()
                    {
                        Id = reader.GetUInt32("id"),
                        FriendId = reader.GetUInt32("friend_id"),
                        Owner = reader.GetUInt32("owner")
                    };
                    FriendsIdList.Add(template.FriendId, template);
                }
            }
        }
    }

    public void Save(MySqlConnection connection, MySqlTransaction transaction)
    {
        if (_removedFriends.Count > 0)
        {
            using (var command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = "DELETE FROM friends WHERE owner = @owner AND friend_id IN(" + string.Join(",", _removedFriends) + ")";
                command.Parameters.AddWithValue("@owner", Owner.Id);
                command.Prepare();
                command.ExecuteNonQuery();
                _removedFriends.Clear();
            }
        }

        foreach (var (_, value) in FriendsIdList)
        {
            using (var command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = "REPLACE INTO friends(`id`,`friend_id`,`owner`) VALUES (@id, @friend_id, @owner)";
                command.Parameters.AddWithValue("@id", value.Id);
                command.Parameters.AddWithValue("@friend_id", value.FriendId);
                command.Parameters.AddWithValue("@owner", value.Owner);
                command.ExecuteNonQuery();
            }
        }
    }
}
