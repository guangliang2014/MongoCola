﻿using System;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoUtility.Basic
{
    public static class ElementHelper
    {
        /// <summary>
        /// </summary>
        public static Object _ClipElement;

        /// <summary>
        /// </summary>
        public static Boolean _IsElementClip = true;

        /// <summary>
        ///     Can Paste As Value
        /// </summary>
        public static Boolean CanPasteAsValue
        {
            get { return (_ClipElement != null && !_IsElementClip); }
        }

        /// <summary>
        ///     Can Paste As Element
        /// </summary>
        public static Boolean CanPasteAsElement
        {
            get { return (_ClipElement != null && _IsElementClip); }
        }

        //http://www.mongodb.org/display/DOCS/Capped+Collections#CappedCollections-UsageandRestrictions
        //Usage and Restrictions Of capped collection.
        //You may insert new documents in the capped collection.
        //You may update the existing documents in the collection. However, the documents must not grow in size. If they do, the update will fail. Note if you are performing updates, you will likely want to declare an appropriate index (given there is no _id index for capped collections by default).
        //The database does not allow deleting documents from a capped collection. Use the drop() method to remove all rows from the collection. (After the drop you must explicitly recreate the collection.)
        //Capped collection are not shard-able.

        /// <summary>
        ///     Paste
        /// </summary>
        /// <param name="ElementPath"></param>
        /// <param name="CurrentDocument"></param>
        public static String PasteElement(String ElementPath, BsonDocument CurrentDocument,
            MongoCollection CurrentCollection)
        {
            var BaseDoc = CurrentDocument;
            var t = GetLastParentDocument(BaseDoc, ElementPath, true);
            if (t.IsBsonDocument)
            {
                try
                {
                    t.AsBsonDocument.InsertAt(t.AsBsonDocument.ElementCount, (BsonElement) _ClipElement);
                }
                catch (InvalidOperationException ex)
                {
                    return ex.Message;
                }
            }
            if (!CurrentCollection.IsCapped())
            {
                CurrentCollection.Save(BaseDoc);
            }
            return String.Empty;
        }

        /// <summary>
        /// </summary>
        /// <param name="ElementPath"></param>
        public static void PasteValue(String ElementPath, BsonDocument CurrentDocument,
            MongoCollection CurrentCollection)
        {
            var BaseDoc = CurrentDocument;
            var t = GetLastParentDocument(BaseDoc, ElementPath, true);
            if (t.IsBsonArray)
            {
                t.AsBsonArray.Insert(t.AsBsonArray.Count, (BsonValue) _ClipElement);
            }
            if (!CurrentCollection.IsCapped())
            {
                CurrentCollection.Save(BaseDoc);
            }
        }

        /// <summary>
        ///     Cut Element
        /// </summary>
        /// <param name="El"></param>
        public static void CopyElement(BsonElement El)
        {
            _ClipElement = El;
            _IsElementClip = true;
        }

        /// <summary>
        ///     Cut Array Value
        /// </summary>
        /// <param name="Val"></param>
        public static void CopyValue(BsonValue Val)
        {
            _ClipElement = Val;
            _IsElementClip = false;
        }

        /// <summary>
        ///     Cut Element
        /// </summary>
        /// <param name="ElementPath"></param>
        public static void CutElement(String ElementPath, BsonElement El, BsonDocument CurrentDocument,
            MongoCollection CurrentCollection)
        {
            _ClipElement = El;
            _IsElementClip = true;
            DropElement(ElementPath, El, CurrentDocument, CurrentCollection);
        }

        /// <summary>
        ///     Cut Array Value
        /// </summary>
        /// <param name="ElementPath"></param>
        public static void CutValue(String ElementPath, int ValueIndex, BsonValue Val, BsonDocument CurrentDocument,
            MongoCollection CurrentCollection)
        {
            _ClipElement = Val;
            _IsElementClip = false;
            DropArrayValue(ElementPath, ValueIndex, CurrentDocument, CurrentCollection);
        }

        /// <summary>
        ///     Add Element
        /// </summary>
        /// <param name="ElementPath"></param>
        /// <param name="AddElement"></param>
        public static String AddElement(String ElementPath, BsonElement AddElement, BsonDocument CurrentDocument,
            MongoCollection CurrentCollection)
        {
            var BaseDoc = CurrentDocument;
            var t = GetLastParentDocument(BaseDoc, ElementPath, true);
            if (t.IsBsonDocument)
            {
                try
                {
                    t.AsBsonDocument.InsertAt(t.AsBsonDocument.ElementCount, AddElement);
                }
                catch (InvalidOperationException ex)
                {
                    return ex.Message;
                }
            }
            if (!CurrentCollection.IsCapped())
            {
                CurrentCollection.Save(BaseDoc);
            }
            return String.Empty;
        }

        /// <summary>
        ///     Add Value
        /// </summary>
        /// <param name="ElementPath"></param>
        /// <param name="AddValue"></param>
        public static void AddArrayValue(String ElementPath, BsonValue AddValue, BsonDocument CurrentDocument,
            MongoCollection CurrentCollection)
        {
            var BaseDoc = CurrentDocument;
            var t = GetLastParentDocument(BaseDoc, ElementPath, true);
            if (t.IsBsonArray)
            {
                t.AsBsonArray.Insert(t.AsBsonArray.Count, AddValue);
            }
            if (!CurrentCollection.IsCapped())
            {
                CurrentCollection.Save(BaseDoc);
            }
        }

        /// <summary>
        ///     Drop Element
        /// </summary>
        /// <param name="ElementPath"></param>
        /// <param name="El"></param>
        public static void DropElement(String ElementPath, BsonElement El, BsonDocument CurrentDocument,
            MongoCollection CurrentCollection)
        {
            var BaseDoc = CurrentDocument;
            var t = GetLastParentDocument(BaseDoc, ElementPath, false);
            if (t.IsBsonDocument)
            {
                t.AsBsonDocument.Remove(El.Name);
            }
            CurrentCollection.Save(BaseDoc);
        }

        /// <summary>
        ///     Drop A Value of Array
        /// </summary>
        /// <param name="ElementPath"></param>
        /// <param name="ValueIndex"></param>
        public static void DropArrayValue(String ElementPath, int ValueIndex, BsonDocument CurrentDocument,
            MongoCollection CurrentCollection)
        {
            var BaseDoc = CurrentDocument;
            var t = GetLastParentDocument(BaseDoc, ElementPath, false);
            if (t.IsBsonArray)
            {
                t.AsBsonArray.RemoveAt(ValueIndex);
            }
            if (!CurrentCollection.IsCapped())
            {
                CurrentCollection.Save(BaseDoc);
            }
        }

        /// <summary>
        ///     Modify Element
        /// </summary>
        /// <param name="ElementPath"></param>
        /// <param name="NewValue"></param>
        /// <param name="El"></param>
        public static void ModifyElement(String ElementPath, BsonValue NewValue, BsonElement El,
            BsonDocument CurrentDocument, MongoCollection CurrentCollection)
        {
            var BaseDoc = CurrentDocument;
            var t = GetLastParentDocument(BaseDoc, ElementPath, false);
            if (t.IsBsonDocument)
            {
                //TODO:需要重新实现
                t.AsBsonDocument.SetElement(new BsonElement(El.Name, NewValue));
            }
            if (!CurrentCollection.IsCapped())
            {
                CurrentCollection.Save(BaseDoc);
            }
        }

        /// <summary>
        ///     Modify A Value of Array
        /// </summary>
        /// <param name="ElementPath"></param>
        /// <param name="NewValue"></param>
        /// <param name="ValueIndex"></param>
        public static void ModifyArrayValue(String ElementPath, BsonValue NewValue, int ValueIndex,
            BsonDocument CurrentDocument, MongoCollection CurrentCollection)
        {
            var BaseDoc = CurrentDocument;
            var t = GetLastParentDocument(BaseDoc, ElementPath, false);
            if (t.IsBsonArray)
            {
                t.AsBsonArray[ValueIndex] = NewValue;
            }
            if (!CurrentCollection.IsCapped())
            {
                CurrentCollection.Save(BaseDoc);
            }
        }

        /// <summary>
        ///     Locate the Operation Place
        /// </summary>
        /// <param name="BaseDoc"></param>
        /// <param name="ElementPath"></param>
        /// <param name="IsGetLast">T:GetOperationPlace F:GetOperationPlace Parent</param>
        /// <returns></returns>
        public static BsonValue GetLastParentDocument(BsonDocument BaseDoc, String ElementPath, Boolean IsGetLast)
        {
            BsonValue Current = BaseDoc;
            //JpCnWord[1]\Translations[ARRAY]\Translations[1]\Sentences[ARRAY]\Sentences[1]\Japanese:"ああいう文章はなかなか書けない"
            //1.将路径按照\分开
            var strPath = ElementPath.Split(@"\".ToCharArray());
            //JpCnWord[1]                                    First
            //Translations[ARRAY]
            //Translations[1]
            //Sentences[ARRAY]
            //Sentences[1]
            //Japanese:"ああいう文章はなかなか書けない"        Last
            int DeepLv;
            if (IsGetLast)
            {
                DeepLv = strPath.Length;
            }
            else
            {
                DeepLv = strPath.Length - 1;
            }
            for (var i = 1; i < DeepLv; i++)
            {
                var strTag = strPath[i];
                var IsArray = false;
                if (strTag.EndsWith(ConstMgr.Array_Mark))
                {
                    //去除[Array]后缀
                    strTag = strTag.Substring(0, strTag.Length - ConstMgr.Array_Mark.Length);
                    IsArray = true;
                }
                if (IsArray)
                {
                    //这里的Array是指一个列表的上层节点，在BSON里面没有相应的对象，只是个逻辑概念
                    if (strTag == String.Empty)
                    {
                        //Array里面的Array,所以没有元素名称。
                        //TODO：正确做法是将元素的Index传入，这里暂时认为第一个数组就是目标数组
                        foreach (var item in Current.AsBsonArray)
                        {
                            if (item.IsBsonArray)
                            {
                                Current = item;
                            }
                        }
                    }
                    else
                    {
                        Current = Current.AsBsonDocument.GetValue(strTag).AsBsonArray;
                    }
                }
                else
                {
                    if (Current.IsBsonArray)
                    {
                        //当前的如果是数组，获得当前下标。
                        int Index =
                            Convert.ToInt16(strTag.Substring(strTag.IndexOf("[") + 1,
                                strTag.Length - strTag.IndexOf("[") - 2));
                        Current = Current.AsBsonArray[Index - 1];
                    }
                    else
                    {
                        if (Current.IsBsonDocument)
                        {
                            //如果当前还是一个文档的话
                            Current = Current.AsBsonDocument.GetValue(strTag);
                        }
                        else
                        {
                            //不应该会走到这个分支
                            return null;
                        }
                    }
                }
            }
            return Current;
        }
    }
}