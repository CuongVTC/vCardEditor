﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using Thought.vCards;
using VCFEditor.Model;
using System.ComponentModel;
using vCardEditor.Repository;
using System.Collections.Generic;

namespace VCFEditor.Repository
{
    public class ContactRepository : IContactRepository
    {
        public string fileName { get; set; }
        private IFileHandler _fileHandler;
        #region Contact Info
        /// <summary>
        /// Formatted name.
        /// </summary>
        public const string KeyName = "FN";

        /// <summary>
        /// Keep a copy of contact list when filtering
        /// </summary>
        private BindingList<Contact> OriginalContactList = null;
        /// <summary>
        /// Contact List
        /// </summary>
        private BindingList<Contact> _contacts;
        public BindingList<Contact> Contacts
        {
            get
            {
                if (_contacts == null)
                    _contacts = new BindingList<Contact>();
                return _contacts;
            }
            set
            {
                _contacts = value;
            }
        }
        #endregion

        public ContactRepository(IFileHandler fileHandler)
        {
            _fileHandler = fileHandler;
        }
        /// <summary>
        /// Load the contacts from filename. 
        /// 1- Parse the file
        /// 2- 
        /// </summary>
        /// <param name="path"></param>
        public BindingList<Contact> LoadContacts(string fileName)
        {
            this.fileName = fileName;

            StringBuilder RawContent = new StringBuilder();
            Contact contact = new Contact();
            string[] lines = _fileHandler.ReadAllLines(fileName);

            //Prevent from adding contacts to existings ones.
            Contacts.Clear();

            for (int i = 0; i < lines.Length; i++)
            {
                RawContent.AppendLine(lines[i]);
                if (lines[i] == "END:VCARD")
                {
                    contact.card = ParseRawContent(RawContent);
                    Contacts.Add(contact);
                    contact = new Contact();
                    RawContent.Length = 0;
                }
              
            }

            OriginalContactList = Contacts;
            return Contacts;
        }

        /// <summary>
        /// Save the contact to the file.
        /// </summary>
        /// <param name="path">Path to the new file, else if null, we overwrite the same file</param>
        public void SaveContacts(string fileName)
        {
            //overwrite the same file, else save as another file.
            if (string.IsNullOrEmpty(fileName))
                fileName = this.fileName;

            //Take a copy...
            if (!ConfigRepository.Instance.OverWrite)
                File.Move(fileName, fileName + ".old");

            StringBuilder sb = new StringBuilder();
            //Do not save the deleted ones...
            foreach (var entry in Contacts)
            {
                if (!entry.isDeleted)
                    sb.Append(generateRawContent(entry.card));
            }
                

            _fileHandler.WriteAllText(fileName, sb.ToString());
        }


        /// <summary>
        /// Delete contacted that are selected.
        /// </summary>
        public void DeleteContact()
        {
            if (_contacts != null && _contacts.Count > 0)
            {
                //loop from the back to prevent index mangling...
                for (int i = _contacts.Count - 1; i > -1; i--)
                {
                    if (_contacts[i].isSelected)
                    {
                        _contacts[i].isDeleted = true;
                        _contacts.RemoveAt(i);
                        dirty = true;
                    }
                        
                }
            }

        }


        /// <summary>
        /// Use the lib to parse a vcard chunk.
        /// </summary>
        /// <param name="rawContent"></param>
        /// <returns></returns>
        private vCard ParseRawContent(StringBuilder rawContent)
        {
            vCard card = null;

            using (MemoryStream s = GenerateStreamFromString(rawContent.ToString()))
            using (TextReader streamReader = new StreamReader(s, Encoding.UTF8))
            {
                card = new vCard(streamReader);
            }

            return card;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private MemoryStream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public BindingList<Contact> FilterContacts(string filter)
        {
            var list = OriginalContactList.Where(i => (i.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) && 
                                                    !i.isDeleted);
            Contacts = new BindingList<Contact>(list.ToList());
            return Contacts;
        }


        /// <summary>
        /// Save modified card info in the raw content.
        /// </summary>
        /// <param name="card"></param>
        /// <param name="index"></param>
        public void SaveDirtyFlag(int index)
        {
            if (index > -1)
                _contacts[index].isDirty = true;
        }

        public void SaveDirtyVCard(int index, vCard NewCard)
        {
            if (index > -1 && _contacts[index].isDirty)
            {
                vCard card = _contacts[index].card;
                card.FormattedName = NewCard.FormattedName;

                SavePhone(NewCard, card);
                SaveEmail(NewCard, card);
                SaveWebUrl(NewCard, card);

                _contacts[index].isDirty = false;
                _dirty = false;
            }
        }


        private void SaveWebUrl(vCard NewCard, vCard card)
        {
            var type = typeof(vCardWebsiteTypes);
            var names = Enum.GetNames(type);

            foreach (var name in names)
            {
                var urlType = (vCardWebsiteTypes)Enum.Parse(type, name);

                if (NewCard.Websites.GetFirstChoice(urlType) != null)
                {
                    if (card.Websites.GetFirstChoice(urlType) != null)
                        card.Websites.GetFirstChoice(urlType).Url = NewCard.Websites.GetFirstChoice(urlType).Url;
                    else
                        card.Websites.Add(new vCardWebsite(NewCard.Websites.GetFirstChoice(urlType).Url, urlType));
                }
                else
                {
                    if (card.Websites.GetFirstChoice(urlType) != null)
                        card.Websites.GetFirstChoice(urlType).Url = string.Empty;

                }
            }
        }

        private void SavePhone(vCard NewCard, vCard card)
        {
            var type = typeof(vCardPhoneTypes);
            var names = Enum.GetNames(type);

            foreach (var name in names)
            {
                var phoneType = (vCardPhoneTypes)Enum.Parse(type, name);
                
                if (NewCard.Phones.GetFirstChoice(phoneType) != null)
                {
                    if (card.Phones.GetFirstChoice(phoneType) != null)
                        card.Phones.GetFirstChoice(phoneType).FullNumber = NewCard.Phones.GetFirstChoice(phoneType).FullNumber;
                    else
                        card.Phones.Add(new vCardPhone(NewCard.Phones.GetFirstChoice(phoneType).FullNumber, phoneType));
                }
                else
                {
                    if (card.Phones.GetFirstChoice(phoneType) != null)
                        card.Phones.GetFirstChoice(phoneType).FullNumber = string.Empty;

                }
            }
        }

        private void SaveEmail(vCard NewCard, vCard card)
        {
            var type = typeof(vCardEmailAddressType);
            var names = Enum.GetNames(type);

            foreach (var name in names)
            {
                var emailType = (vCardEmailAddressType)Enum.Parse(type, name);

                if (NewCard.EmailAddresses.GetFirstChoice(emailType) != null)
                {
                    if (card.EmailAddresses.GetFirstChoice(emailType) != null)
                        card.EmailAddresses.GetFirstChoice(emailType).Address = NewCard.EmailAddresses.GetFirstChoice(emailType).Address;
                    else
                        card.EmailAddresses.Add(new vCardEmailAddress(NewCard.EmailAddresses.GetFirstChoice(emailType).Address,
                            emailType));
                }
                else
                {
                    if (card.EmailAddresses.GetFirstChoice(emailType) != null)
                        card.EmailAddresses.GetFirstChoice(emailType).Address = string.Empty;

                }
            }

        }

        /// <summary>
        /// Generate a VCard class from a string.
        /// </summary>
        /// <param name="card"></param>
        /// <returns></returns>
        private string generateRawContent(vCard card)
        {
            vCardStandardWriter writer = new vCardStandardWriter();
            TextWriter tw = new StringWriter();
            writer.Write(card, tw);

            return tw.ToString();
        }

        /// <summary>
        /// Check if some iem in the contact list is modified
        /// </summary>
        /// <returns>true for dirty</returns>
        private bool _dirty;
        public bool dirty
        {
            get { return _dirty || (_contacts != null && _contacts.Any(x => x.isDirty)); }
            set { _dirty = value; }
        }

    }
}
