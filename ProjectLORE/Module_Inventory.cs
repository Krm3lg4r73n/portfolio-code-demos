using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Lore
{
    [RequireComponent(typeof(TriggerObject))]
    [AddComponentMenu("Lore NetObject/Modules/Inventory")]
    public sealed class Module_Inventory : ModuleBase, IInteractable, ITriggerProvider
    {
        [SerializeField]
        private SphereCollider m_InteractionSphere;
        public SphereCollider interactionSphere
        {
            get { return m_InteractionSphere; }
        }

        [SerializeField]
        private InventoryReference m_InventoryRef;
        public ushort inventoryId { get { return m_InventoryRef.id; } }

        private List<uint> m_InteractingActors;

        private Events_Trigger.ItemAdded e_ItemAdded;
        private Events_Trigger.ItemRemoved e_ItemRemoved;
        private Messages.ModifyInventory m_ModifyPayload;

        public event System.Action<uint> onStart_Server;
        public event System.Action<uint> onStop_Server;

        public ETriggerObjectType mask
        {
            get { return ETriggerObjectType.Interaction | ETriggerObjectType.Inventory; }
        }


        //Shared
        private void Awake()
        {
            if(GameManager.i.server != null)
            {
                m_InteractingActors = new List<uint>();
                e_ItemAdded = new Events_Trigger.ItemAdded();
                e_ItemAdded.inventoryId = m_InventoryRef.id;
                e_ItemRemoved = new Events_Trigger.ItemRemoved();
                e_ItemRemoved.inventoryId = m_InventoryRef.id;
                m_ModifyPayload = new Messages.ModifyInventory();

                GameManager.i.itemSystem.RegisterInventoryHandlers(m_InventoryRef.id,
                    OnItemAddedRemoved, OnInventoryUpdated);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.isQuitting)
                return;

            if (GameManager.i.server != null)
            {
                GameManager.i.itemSystem.UnregisterInventoryHandlers(m_InventoryRef.id,
                    OnItemAddedRemoved, OnInventoryUpdated);
            }
        }

        public override void HandleIdentityMessage(Messages.IdentityMessage.Payload payload)
        {
            if (GameManager.i.client != null)
            {
                if (payload is Messages.OpenInventory)
                {
                    var data = payload as Messages.OpenInventory;
                    GlobalEventManager.i.InvokeEvent(new Events_UI.OpenInventory
                    {
                        type = EInventoryType.World,
                        inventory = data.inventory
                    });
                }
                else if (payload is Messages.StopInteration)
                {
                    GlobalEventManager.i.InvokeEvent(new Events_UI.CloseInventory
                    {
                        type = EInventoryType.World
                    });
                }
                else if (payload is Messages.ModifyInventory)
                {
                    var data = payload as Messages.ModifyInventory;
                    GlobalEventManager.i.InvokeEvent(new Events_UI.ModifyInventory
                    {
                        type = EInventoryType.World,
                        slotIndex = data.slotIndex,
                        itemRef = data.itemRef
                    });
                }
            }
        }


        //Server
        private void OnItemAddedRemoved(ItemReference itemRef, EInvModification mod)
        {
            if (m_TriggerEvents)
            {
                switch (mod)
                {
                    case EInvModification.Added:
                        e_ItemAdded.itemReference = itemRef;
                        GlobalEventManager.i.InvokeEvent(e_ItemAdded);
                        break;
                    case EInvModification.Removed:
                        e_ItemRemoved.itemReference = itemRef;
                        GlobalEventManager.i.InvokeEvent(e_ItemRemoved);
                        break;
                }
            }
        }

        public void OnInventoryUpdated(short slotIndex, ItemReference itemRef)
        {
            foreach (var actor in m_InteractingActors)
            {
                var playerData = GameManager.i.server.playerManager.GetPlayerData(actor);
                if (playerData != null)
                {
                    m_ModifyPayload.slotIndex = slotIndex;
                    m_ModifyPayload.itemRef = itemRef;
                    this.SendIdentityMessage(playerData.connectionId, m_ModifyPayload);
                }
            }
        }

        public void RequestPlayerInteraction(uint playerNetId)
        {
            var pd = GameManager.i.server.playerManager.GetPlayerData(playerNetId);
            if (pd != null)
            {
                pd.player.RequestInteraction(this);
            }
        }

        public void OnStartInteraction(IEnumerable<uint> actorIds)
        {
            foreach (var actorNetId in actorIds)
            {
                if (this.onStart_Server != null)
                    this.onStart_Server(actorNetId);

                if (!m_InteractingActors.Contains(actorNetId))
                    this.m_InteractingActors.Add(actorNetId);
                
                if (m_TriggerEvents)
                {
                    GlobalEventManager.i.InvokeEvent(new Events_Trigger.InteractionStateChanged
                    {
                        state = Events_Trigger.InteractionStateChanged.EState.InteractionStarted,
                        interactable = this.netIdentity,
                        actor = GameManager.i.server.GetNetworkIdentity(actorNetId),
                        tool = ItemReference.CreateInvalid()
                    });
                }

                //send inventory to client if actor is player
                var playerData = GameManager.i.server.playerManager.GetPlayerData(actorNetId);
                if (playerData != null)
                {
                    var inv = GameManager.i.itemSystem.GetInventory(m_InventoryRef.id);

                    DebugHelper.Assert(inv != null,
                        "Object '" + gameObject.name + "' has an invalid inventory id.");

                    this.SendIdentityMessage(playerData.connectionId,
                        new Messages.OpenInventory { inventory = inv });
                }
            }
        }

        public void OnStopInteraction(IEnumerable<uint> actorIds)
        {
            foreach (var actorNetId in actorIds)
            {
                if (this.onStop_Server != null)
                    this.onStop_Server(actorNetId);

                if (m_InteractingActors.Contains(actorNetId))
                    this.m_InteractingActors.Remove(actorNetId);

                var playerData = GameManager.i.server.playerManager.GetPlayerData(actorNetId);
                if (playerData != null)
                    this.SendIdentityMessage(playerData.connectionId, new Messages.StopInteration());
            }
        }

        public void StopAllPlayerInteractions()
        {
            var toRemove = new List<uint>();
            foreach (var actorNetId in m_InteractingActors)
            {
                var playerData = GameManager.i.server.playerManager.GetPlayerData(actorNetId);
                if (playerData != null)
                {
                    if (this.onStop_Server != null)
                        this.onStop_Server(actorNetId);

                    toRemove.Add(actorNetId);
                    this.SendIdentityMessage(playerData.connectionId, new Messages.StopInteration());
                }
            }

            foreach (var item in toRemove)
                m_InteractingActors.Remove(item);
        }
    }
}