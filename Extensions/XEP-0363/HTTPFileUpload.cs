﻿using Net.Xmpp.Im;
using System;
using System.Collections.Generic;

namespace Net.Xmpp.Extensions
{
    /// <summary>
    /// Provide HTTP File Upload capabilities
    /// </summary>
    internal class HTTPFileUpload : XmppExtension
    {
        private const string xmlns = "urn:xmpp:http:upload:0";

        /// <summary>
        /// A reference to the 'Service Discovery' extension instance.
        /// </summary>
        private ServiceDiscovery sdisco;

        public override IEnumerable<string> Namespaces
        {
            get { return new string[] { xmlns }; }
        }

        public override Extension Xep
        {
            get { return Extension.HTTPUpload; }
        }

        public HTTPFileUpload(XmppIm im)
            : base(im)
        {
        }

        /// <summary>
        /// Invoked after all extensions have been loaded.
        /// </summary>
        public override void Initialize()
        {
            sdisco = im.GetExtension<ServiceDiscovery>();
        }

        /// <summary>
        /// Request URL and quota for upload data
        /// </summary>
        /// <param name="fileName">
        /// The name of the file that will be uploaded
        /// </param>
        /// <param name="size">
        /// The file size
        /// </param>
        /// <param name="contentType">
        /// The MIME content type of the file
        /// </param>
        /// <param name="upload">
        /// Callback with Slot information
        /// </param>
        /// <param name="error">
        /// Callback in case of fail
        /// </param>
        public void RequestSlot(string fileName, long size, string contentType, Action<Slot> upload, Action<String> error)
        {
            Jid uploadDomain = null;
            foreach (var item in sdisco.GetItems(im.Jid.Domain))
            {
                // Query each item for its identities and look for a 'store' identity.
                foreach (var ident in sdisco.GetIdentities(item.Jid))
                {
                    if (ident.Category == "store" && ident.Type == "file")
                    {
                        uploadDomain = item.Jid;
                    }
                }
            }

            if (uploadDomain == null)
            {
                throw new Exception("Service Unavaible");
            }
            string id = Guid.NewGuid().ToString("N");
            SlotRequest request = new SlotRequest(fileName, size, contentType);
            im.IqRequestAsync(Core.IqType.Get, uploadDomain, null, request.ToXmlElement(), null,
                (string result, Core.Iq response) =>
                {
                    if (response.Type == Core.IqType.Error)
                    {
                        error(response.Data["error"]["text"].InnerText);
                    }
                    else
                    {
                        upload(new Slot(response.Data["slot"]));
                    }
                });
        }
    }
}
