using RSBot.Core.Components;
using RSBot.Core.Event;
using RSBot.Core.Objects;
using RSBot.Core.Objects.Quests;

namespace RSBot.Core.Network.Handler.Agent.Character;

internal class CharacterDataEndResponse : IPacketHandler
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x34A6;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        SpawnManager.Clear();

        packet = Game.ChunkedPacket;
        packet.Lock();

        bool isJSRO = Game.ClientType == GameClientType.jSRO;

        if (Game.ClientType >= GameClientType.Thailand)
            packet.ReadUInt(); // serverTimestamp

        var modelId = packet.ReadUInt(); // RefObjID

        var character = new Player(modelId);

        character.Scale = packet.ReadByte();
        character.Level = packet.ReadByte();
        character.MaxLevel = packet.ReadByte();

        character.Experience = packet.ReadLong();
        character.SkillExperience = packet.ReadUInt();

        character.Gold = packet.ReadULong();
        character.SkillPoints = packet.ReadUInt();

        character.StatPoints = packet.ReadUShort();
        character.BerzerkPoints = packet.ReadByte();

        character.ExperienceChunk = packet.ReadUInt();

        character.Health = packet.ReadInt();
        character.Mana = packet.ReadInt();
        character.AutoInverstExperience = (AutoInverstType)packet.ReadByte();

        if (Game.ClientType == GameClientType.Chinese_Old)
            character.DailyPK = (byte)packet.ReadUShort();
        else
            character.DailyPK = packet.ReadByte();

        character.TotalPK = packet.ReadUShort();
        character.PKPenaltyPoint = packet.ReadUInt();

        if (Game.ClientType >= GameClientType.Thailand)
            character.BerzerkLevel = packet.ReadByte();

        if (Game.ClientType > GameClientType.Thailand)
            /*character.PvpFlag = (PvpFlag)*/
            packet.ReadByte(); // < not in jSRO

        if (Game.ClientType >= GameClientType.Chinese)
        {
            if (Game.ClientType != GameClientType.Chinese)
                packet.ReadByte();

            packet.ReadUInt(); //You can use VIP service until this time
            packet.ReadByte();

            if (
                Game.ClientType == GameClientType.Turkey
                || Game.ClientType == GameClientType.VTC_Game
                || Game.ClientType == GameClientType.RuSro
                || Game.ClientType == GameClientType.Taiwan
            )
                packet.ReadUInt();

            if (Game.ClientType == GameClientType.Rigid)
                packet.ReadBytes(12);

            if (Game.ClientType == GameClientType.VTC_Game)
                packet.ReadByte(); // ??

            if (Game.ClientType == GameClientType.Taiwan)
                packet.ReadBytes(5);

            var serverCap = packet.ReadByte();
            Log.Notify($"The game server cap is {serverCap}!");

            if (
                Game.ClientType != GameClientType.Korean
                && Game.ClientType != GameClientType.Chinese
                && Game.ClientType != GameClientType.Japanese
            )
                packet.ReadUShort();
        }

        Log.Notify($"Health = {character.Health} Mana = {character.Mana} autoInverstExp = {character.AutoInverstExperience}");
        Log.Notify($"DailyPK = {character.DailyPK} TotalPK = {character.TotalPK} PKPenaltyPoint = {character.PKPenaltyPoint}");

        character.Inventory = new CharacterInventory(packet);

        Log.Notify("Guess inventory packet is fine?");

        if (Game.ClientType >= GameClientType.Thailand)
            character.Avatars = new InventoryItemCollection(packet);
        else
            character.Avatars = new InventoryItemCollection(5);

        // JOB2
        if (!isJSRO && Game.ClientType > GameClientType.Vietnam)
        {
            character.Job2SpecialtyBag = new InventoryItemCollection(packet);

            character.Job2 = new InventoryItemCollection(packet);
        }

        character.Skills = Skills.FromPacket(packet);


        character.QuestLog = QuestLog.FromPacket(packet);

        packet.ReadByte(); // Unknown

        if (!isJSRO && Game.ClientType > GameClientType.Thailand)
        {
            var collectionBookStartedThemeCount = packet.ReadUInt();
            for (var i = 0; i < collectionBookStartedThemeCount; i++)
            {
                packet.ReadUInt(); //index
                packet.ReadUInt(); //Starttime
                packet.ReadUInt(); //pages
            }
        }

        // Movement and UniqueCharID
        character.ParseBionicDetails(packet);

        character.Name = packet.ReadString();
        character.JobInformation = JobInfo.FromPacket(packet);
        character.State.PvpState = (PvpState)packet.ReadByte();
        Log.Warn($"character.State.PvpState = {character.State.PvpState}");
        character.OnTransport = packet.ReadBool(); //On transport?
        character.InCombat = packet.ReadBool();

        if (Game.ClientType == GameClientType.jSRO)
            goto JsonContinue;

        if (Game.ClientType >= GameClientType.Chinese)
            packet.ReadByte();

        if (character.OnTransport)
            character.TransportUniqueId = packet.ReadUInt();

        if (Game.ClientType >= GameClientType.Chinese)
            packet.ReadUInt(); //unkUint2 i think it is using for balloon event or buff for events

        if (Game.ClientType > GameClientType.Vietnam)
            packet.ReadByte();

        packet.ReadByte(); //PVP dress for the CTF event //0 = Red Side, 1 = Blue Side, 0xFF = None

        if (
            Game.ClientType > GameClientType.Chinese
            && Game.ClientType != GameClientType.Global
            && Game.ClientType != GameClientType.Rigid
            && Game.ClientType != GameClientType.RuSro
            && Game.ClientType != GameClientType.Korean
            && Game.ClientType != GameClientType.VTC_Game
            && Game.ClientType != GameClientType.Japanese
        )
        {
            packet.ReadByte(); // 0xFF
            packet.ReadUShort(); // 0xFF
            packet.ReadUShort(); // 0xFF
        }

    JsonContinue:

        if (Game.ClientType == GameClientType.jSRO)
        {
            if (character.OnTransport)
                character.TransportUniqueId = packet.ReadUInt();

            packet.ReadByte(); //PVP dress for the CTF event //0 = Red Side, 1 = Blue Side, 0xFF = None
        }

        //GuideFlag
        if (Game.ClientType == GameClientType.jSRO || Game.ClientType >= GameClientType.Thailand)
            packet.ReadULong();
        else
            packet.ReadUInt();



        if (
            Game.ClientType == GameClientType.Chinese_Old
            || Game.ClientType == GameClientType.Chinese
            || Game.ClientType == GameClientType.Global
            || Game.ClientType == GameClientType.RuSro
            || Game.ClientType == GameClientType.Korean
            || Game.ClientType == GameClientType.VTC_Game
            || Game.ClientType == GameClientType.Japanese
        )
            packet.ReadByte();


        if (Game.ClientType == GameClientType.Chinese)
            packet.ReadByte();

        if (Game.ClientType == GameClientType.jSRO)
        {
            character.IsGameMaster = packet.ReadBool();
        }
        else
        {
            character.JID = packet.ReadUInt();
            character.IsGameMaster = packet.ReadBool();
        }

        // Load Notification sound settings
        character.NotificationSounds.LoadPlayerSettings();

        //Set instance..
        Game.Player = character;
        Game.ChunkedPacket = null;

        EventManager.FireEvent("OnLoadCharacter");

        ClientManager.SetTitle($"{character.Name} - RSBot");

        if (!Game.Clientless)
            return;

        PacketManager.SendPacket(new Packet(0x3012), PacketDestination.Server);
        Game.Ready = true;
    }
}
