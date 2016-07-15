using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace Lore
{
    public enum EInvModification
    {
        Added,
        Removed
    }

    public delegate bool ItemValidator(ItemTemplate item, short slotIndex);
    public delegate void ItemAddedRemovedHandler(ItemReference itemRef, EInvModification mod);
    public delegate void InventoryUpdateHandler(short slotIndex, ItemReference itemRef);

    [System.Serializable]
    public class Inventory
    {
        public const ushort INVENTORY_ID_PSEUDO_GROUND = 0;
        public const ushort INVENTORY_ID_INVALID = 1;
        public const ushort INVENTORY_ID_FIRST_FREE = 2;

        [System.NonSerialized]
        public ItemValidator itemAddedValidator = DefaultItemValidator;
        [System.NonSerialized]
        public ItemValidator itemRemovedValidator = DefaultItemValidator;

        [field: System.NonSerialized]
        public event ItemAddedRemovedHandler onItemAddedRemoved;

        [field: System.NonSerialized]
        public event InventoryUpdateHandler onInventoryUpdate;

        public ushort id;
        public string name;
        public ItemReference[] items;
        public short capacity;

        public short nextFreeSlot
        {
            get
            {
                for (short i = 0; i < items.Length; i++)
                {
                    if (!items[i].isValid)
                        return i;
                }
                return -1;
            }
        }

        public static Inventory Create(ushort id, short capacity = 10)
        {
            var inv = new Inventory();

            inv.id = id;
            inv.name = "Inventory" + inv.id;
            inv.Resize(capacity);

            return inv;
        }

        public static bool DefaultItemValidator(ItemTemplate item, short slotIndex) { return true; }

        public void Resize(short capacity)
        {
            this.capacity = capacity;
            System.Array.Resize<ItemReference>(ref items, capacity);
            for(int i = 0; i < items.Length; i++)
            {
                if(items[i] == null)
                    items[i] = ItemReference.CreateInvalid();
            }
        }


        public ItemReference GetItem(short slotIndex)
        {
            if (slotIndex < 0 && slotIndex >= capacity)
                return ItemReference.CreateInvalid();

            return items[slotIndex];
        }

        /// <summary>
        /// Add items to a specific slot in the inventory. Does NO validation.
        /// </summary>
        /// <param name="onlyFillStack">
        /// If true only adds to the current stack; if false only adds if the slot is empty
        /// </param>
        /// <returns>Number of items that were added</returns>
        public uint AddItem(short slotIndex, ItemReference item, bool onlyFillStack)
        {
            if (slotIndex < 0 && slotIndex >= capacity)
                return 0;

            if(onlyFillStack)
            {
                if (!items[slotIndex].isValid || items[slotIndex].id != item.id)
                    return 0;
                
                uint added = (uint)Mathf.Min(
                    items[slotIndex].template.stackSize - items[slotIndex].multiValue,
                    item.multiValue);

                if (added < item.multiValue)
                {
                    items[slotIndex] = new ItemReference(item.id, (uint)items[slotIndex].template.stackSize);
                    
                    if (this.onItemAddedRemoved != null)
                        this.onItemAddedRemoved.Invoke(new ItemReference(item.id, added), 
                            EInvModification.Added);
                }
                else
                {
                    items[slotIndex] = new ItemReference(item.id, items[slotIndex].multiValue + item.multiValue);

                    if (this.onItemAddedRemoved != null)
                        this.onItemAddedRemoved.Invoke(item, EInvModification.Added);
                }


                if (this.onInventoryUpdate != null)
                    this.onInventoryUpdate.Invoke(slotIndex, items[slotIndex]);

                return added;
            }
            else
            {
                if (items[slotIndex].isValid)
                    return 0;
                
                items[slotIndex] = item;
                
                if (this.onItemAddedRemoved != null)
                    this.onItemAddedRemoved.Invoke(item, EInvModification.Added);

                if (this.onInventoryUpdate != null)
                    this.onInventoryUpdate.Invoke(slotIndex, items[slotIndex]);

                return item.multiValue;
            }
        }

        /// <summary>
        /// Remove items from a specific slot in the inventory. Does NO validation.
        /// </summary>
        /// <returns>Number of items that were removed</returns>
        public uint RemoveItem(short slotIndex, uint count)
        {
            if (slotIndex < 0 && slotIndex >= capacity)
                return 0;

            if (!items[slotIndex].isValid)
                return 0;

            if (count >= items[slotIndex].multiValue)
            {
                var tmp = items[slotIndex];
                items[slotIndex] = ItemReference.CreateInvalid();

                if (this.onItemAddedRemoved != null)
                    this.onItemAddedRemoved.Invoke(tmp, EInvModification.Removed);

                if (this.onInventoryUpdate != null)
                    this.onInventoryUpdate.Invoke(slotIndex, items[slotIndex]);

                return tmp.multiValue;
            }
            else
            {
                items[slotIndex] = new ItemReference(items[slotIndex].id,
                    items[slotIndex].multiValue - count);

                if (this.onItemAddedRemoved != null)
                    this.onItemAddedRemoved.Invoke(
                        new ItemReference(items[slotIndex].id, count),
                        EInvModification.Removed);

                if (this.onInventoryUpdate != null)
                    this.onInventoryUpdate.Invoke(slotIndex, items[slotIndex]);

                return count;
            }
        }

        /// <summary>
        /// Add items to any possible slot in the inventory. Does validation.
        /// </summary>
        /// <returns>Number of items that were added</returns>
        public uint AddItem(ItemReference item)
        {
            ItemReference left = item;
            uint added = 0;

            //add to stack pass
            for (short i = 0; i < items.Length; i++)
            {
                if (!this.itemAddedValidator(item.template, i))
                    continue; //not allowed to add to this slot

                added += this.AddItem(i, left, true);

                if(added >= item.multiValue)
                    return item.multiValue;

                left = new ItemReference(item.id, item.multiValue - added);
            }

            //add to empty slot pass
            for (short i = 0; i < items.Length; i++)
            {
                if (!this.itemAddedValidator(item.template, i))
                    continue; //not allowed to add to this slot

                if (this.AddItem(i, left, false) > 0)
                {
                    return item.multiValue;
                }
            }

            return added;
        }

        

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(id);
            writer.Write(name);
            writer.Write(capacity);
            for(int i = 0; i < capacity; i++)
            {
                ItemReference.Serialize(writer, items[i]);
            }
        }

        public void Deserialize(NetworkReader reader)
        {
            id = reader.ReadUInt16();
            name = reader.ReadString();
            capacity = reader.ReadInt16();
            items = new ItemReference[capacity];

            for(int i = 0; i < capacity; i++)
            {
                items[i] = ItemReference.Deserialize(reader);
            }
        }

        public override string ToString()
        {
            return "Inventory " + this.id + ": \"" + this.name + "\"";
        }
    }
}