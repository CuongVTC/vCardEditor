﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thought.vCards;
using VCFEditor.Model;
using System.ComponentModel;
using vCardEditor.View;

namespace VCFEditor.Repository
{
    public interface IContactRepository
    {
        bool dirty { get; }
        string fileName { get; set; }
        SortableBindingList<Contact> Contacts { get; set; }
        SortableBindingList<Contact> LoadContacts(string fileName);
        SortableBindingList<Contact> FilterContacts(string p);
        void SaveContactsToFile(string fileName);
        void DeleteContact();
        void SetDirtyFlag(int index);
        void SaveDirtyVCard(int index, vCard card);
        void AddEmptyContact();
        void ModifyImage(int index, vCardPhoto photo);
        string GetExtension(string path);
        void SaveImageToDisk(string imageFile, vCardPhoto image);

        string GenerateStringFromVCard(vCard card);
    }
}
