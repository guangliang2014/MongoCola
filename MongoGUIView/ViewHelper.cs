﻿using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoGUICtl;
using MongoUtility.Aggregation;
using MongoUtility.Basic;
using MongoUtility.Core;
using MongoUtility.Extend;
using ResourceLib.Utility;

namespace MongoGUIView
{
    public static class ViewHelper
    {
        #region"展示数据集内容[WebForm]"

        /// <summary>
        ///     展示数据
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="controls"></param>
        /// <param name="CurrentDataViewInfo"></param>
        public static void FillDataToControl(List<BsonDocument> dataList, List<Control> controls,
            DataViewInfo CurrentDataViewInfo)
        {
            var collectionPath = CurrentDataViewInfo.strDBTag.Split(":".ToCharArray())[1];
            var cp = collectionPath.Split("/".ToCharArray());
            foreach (var control in controls)
            {
                if (control.GetType() == typeof (ListView))
                {
                    if (
                        !(dataList.Count == 0 &&
                          CurrentDataViewInfo.strDBTag.Split(":".ToCharArray())[0] == ConstMgr.COLLECTION_TAG))
                    {
                        //只有在纯数据集的时候才退出，不然的话，至少需要将字段结构在ListView中显示出来。
                        FillDataToListView(cp[(int) EnumMgr.PathLv.CollectionLv], (ListView) control, dataList);
                    }
                }
                if (control.GetType() == typeof (TextBox))
                {
                    FillJSONDataToTextBox((TextBox) control, dataList, CurrentDataViewInfo.SkipCnt);
                }
                if (control.GetType() == typeof (ctlTreeViewColumns))
                {
                    UIHelper.FillDataToTreeView(cp[(int) EnumMgr.PathLv.CollectionLv], (ctlTreeViewColumns) control,
                        dataList,
                        CurrentDataViewInfo.SkipCnt);
                }
            }
        }

        /// <summary>
        ///     字符转Bsonvalue
        /// </summary>
        /// <param name="strData"></param>
        /// <returns></returns>
        public static BsonValue ConvertFromString(string strData)
        {
            //以引号开始结尾的，解释为字符串
            if (strData.StartsWith("\"") && strData.EndsWith("\""))
            {
                return new BsonString(strData.Trim("\"".ToCharArray()));
            }
            return new BsonString("");
        }

        /// <summary>
        ///     BsonValue转展示用字符
        /// </summary>
        /// <param name="bsonValue"></param>
        /// <returns></returns>
        public static string ConvertToString(BsonValue bsonValue)
        {
            //二进制数据
            if (bsonValue.IsBsonBinaryData)
            {
                return "[Binary]";
            }
            //空值
            if (bsonValue.IsBsonNull)
            {
                return "[Empty]";
            }
            //文档
            if (bsonValue.IsBsonDocument)
            {
                return bsonValue + "[Contains" + bsonValue.ToBsonDocument().ElementCount + "Documents]";
            }
            //时间
            if (bsonValue.IsBsonDateTime)
            {
                var bsonData = bsonValue.ToUniversalTime();
                //@flydreamer提出的本地化时间要求
                return bsonData.ToLocalTime().ToString();
            }

            //字符
            if (bsonValue.IsString)
            {
                //只有在字符的时候加上""
                return "\"" + bsonValue + "\"";
            }

            //其他
            return bsonValue.ToString();
        }

        /// <summary>
        /// </summary>
        /// <param name="txtData"></param>
        /// <param name="dataList"></param>
        public static void FillJSONDataToTextBox(TextBox txtData, List<BsonDocument> dataList, int SkipCnt)
        {
            txtData.Clear();
            var Count = 1;
            var sb = new StringBuilder();
            foreach (var BsonDoc in dataList)
            {
                sb.AppendLine("/* " + (SkipCnt + Count) + " */");

                sb.AppendLine(BsonDoc.ToJson(Utility.JsonWriterSettings));
                Count++;
            }
            txtData.Text = sb.ToString();
        }

        /// <summary>
        ///     将数据放入ListView中进行展示
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="lstData"></param>
        /// <param name="dataList"></param>
        public static void FillDataToListView(string collectionName, ListView lstData, List<BsonDocument> dataList)
        {
            lstData.Clear();
            lstData.SmallImageList = null;
            switch (collectionName)
            {
                case ConstMgr.COLLECTION_NAME_GFS_FILES:
                    SetGridFileToListView(dataList, lstData);
                    break;
                case ConstMgr.COLLECTION_NAME_USER:
                    SetUserListToListView(dataList, lstData);
                    break;
                default:
                    //普通数据的加载
                    SetDataListToListView(dataList, lstData, collectionName);
                    break;
            }
        }

        /// <summary>
        ///     普通数据的加载
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="lstData"></param>
        /// <param name="collectionName"></param>
        private static void SetDataListToListView(List<BsonDocument> dataList, ListView lstData, string collectionName)
        {
            var _columnlist = new List<string>();
            //可以让_id 不在第一位，昏过去了,很多逻辑需要调整
            var isSystem = OperationHelper.IsSystemCollection(string.Empty, collectionName);
            if (!isSystem)
            {
                _columnlist.Add(ConstMgr.KEY_ID);
                lstData.Columns.Add(ConstMgr.KEY_ID);
            }
            foreach (var docItem in dataList)
            {
                var lstItem = new ListViewItem();
                foreach (var item in docItem.Names)
                {
                    if (!_columnlist.Contains(item))
                    {
                        _columnlist.Add(item);
                        lstData.Columns.Add(item);
                    }
                }
                //Key:_id
                if (!isSystem)
                {
                    BsonElement id;
                    docItem.TryGetElement(ConstMgr.KEY_ID, out id);
                    if (id != null)
                    {
                        lstItem.Text = docItem.GetValue(ConstMgr.KEY_ID).ToString();
                        //这里保存真实的主Key数据，删除的时候使用
                        lstItem.Tag = docItem.GetValue(ConstMgr.KEY_ID);
                    }
                    else
                    {
                        lstItem.Text = "[Empty]";
                        lstItem.Tag = docItem.GetElement(0).Value;
                    }
                }
                else
                {
                    lstItem.Text = docItem.GetValue(_columnlist[0]).ToString();
                }
                //OtherItems
                for (var i = isSystem ? 1 : 0; i < _columnlist.Count; i++)
                {
                    if (_columnlist[i] == ConstMgr.KEY_ID)
                    {
                        continue;
                    }
                    BsonValue val;
                    docItem.TryGetValue(_columnlist[i], out val);
                    lstItem.SubItems.Add(val == null ? "" : ConvertToString(val));
                }
                lstData.Items.Add(lstItem);
            }
            Common.Logic.Utility.ListViewColumnResize(lstData);
        }

        /// <summary>
        ///     用户列表
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="lstData"></param>
        private static void SetUserListToListView(List<BsonDocument> dataList, ListView lstData)
        {
            //2.4以后的用户，没有ReadOnly属性，取而代之的是roles属性
            //这里为了向前兼容暂时保持ReadOnle属性
            //Ref:http://docs.mongodb.org/manual/reference/method/db.addUser/
            lstData.Clear();
            if (!configuration.guiConfig.IsUseDefaultLanguage)
            {
                lstData.Columns.Add("ID");
                lstData.Columns.Add(
                    configuration.guiConfig.MStringResource.GetText(StringResource.TextType.Common_Username));
                lstData.Columns.Add(configuration.guiConfig.MStringResource.GetText(StringResource.TextType.Common_Roles));
                lstData.Columns.Add(
                    configuration.guiConfig.MStringResource.GetText(StringResource.TextType.Common_Password));
                lstData.Columns.Add("userSource");
                lstData.Columns.Add("otherDBRoles");
                lstData.Columns.Add(
                    configuration.guiConfig.MStringResource.GetText(StringResource.TextType.Common_ReadOnly));
            }
            else
            {
                lstData.Columns.Add("ID");
                lstData.Columns.Add("user");
                lstData.Columns.Add("roles");
                lstData.Columns.Add("password");
                lstData.Columns.Add("userSource");
                lstData.Columns.Add("otherDBRoles");
                lstData.Columns.Add("readonly");
            }
            foreach (var docFile in dataList)
            {
                var lstItem = new ListViewItem();
                //ID
                lstItem.Text = docFile.GetValue(ConstMgr.KEY_ID).ToString();
                //User
                lstItem.SubItems.Add(docFile.GetValue("user").ToString());
                //roles
                BsonValue strRoles;
                docFile.TryGetValue("roles", out strRoles);
                lstItem.SubItems.Add(strRoles == null ? "N/A" : strRoles.ToString());
                //密码是Hash表示的，这里没有安全隐患
                //Password和userSource不能同时设置，所以password也可能不存在
                BsonValue strPassword;
                docFile.TryGetValue("pwd", out strPassword);
                lstItem.SubItems.Add(strPassword == null ? "N/A" : strPassword.ToString());
                //userSource
                BsonValue strUserSource;
                docFile.TryGetValue("userSource", out strUserSource);
                lstItem.SubItems.Add(strUserSource == null ? "N/A" : strUserSource.ToString());
                //OtherDBRoles
                BsonValue strOtherDBRoles;
                docFile.TryGetValue("otherDBRoles", out strOtherDBRoles);
                lstItem.SubItems.Add(strOtherDBRoles == null ? "N/A" : strOtherDBRoles.ToString());
                //ReadOnly
                //20130802 roles列表示。ReadOnly可能不存在！
                BsonValue strReadOnly;
                docFile.TryGetValue("readOnly", out strReadOnly);
                lstItem.SubItems.Add(strReadOnly == null ? "N/A" : strReadOnly.ToString());
                lstData.Items.Add(lstItem);
            }
            Common.Logic.Utility.ListViewColumnResize(lstData);
            //lstData.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        /// <summary>
        ///     GFS系统
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="lstData"></param>
        private static void SetGridFileToListView(List<BsonDocument> dataList, ListView lstData)
        {
            lstData.Clear();
            if (!configuration.guiConfig.IsUseDefaultLanguage)
            {
                lstData.Columns.Add(configuration.guiConfig.MStringResource.GetText(StringResource.TextType.GFS_filename));
                lstData.Columns.Add(configuration.guiConfig.MStringResource.GetText(StringResource.TextType.GFS_length));
                lstData.Columns.Add(
                    configuration.guiConfig.MStringResource.GetText(StringResource.TextType.GFS_chunkSize));
                lstData.Columns.Add(
                    configuration.guiConfig.MStringResource.GetText(StringResource.TextType.GFS_uploadDate));
                lstData.Columns.Add(configuration.guiConfig.MStringResource.GetText(StringResource.TextType.GFS_md5));
                //!MONO
                lstData.Columns.Add("ContentType");
            }
            else
            {
                lstData.Columns.Add("filename");
                lstData.Columns.Add("length");
                lstData.Columns.Add("chunkSize");
                lstData.Columns.Add("uploadDate");
                lstData.Columns.Add("MD5");
                //!MONO
                lstData.Columns.Add("ContentType");
            }
            lstData.SmallImageList = GetSystemIcon.IconImagelist;
            lstData.LargeImageList = GetSystemIcon.IconImagelist;
            lstData.TileSize = new Size(200, 100);

            foreach (var docFile in dataList)
            {
                var Filename = docFile.GetValue("filename").ToString();
                var lstItem = new ListViewItem();
                lstItem.ImageIndex = GetSystemIcon.GetIconIndexByFileName(Filename, false);
                lstItem.Text = Filename;
                lstItem.ToolTipText = Filename;
                lstItem.SubItems.Add(Utility.GetBsonSize(docFile.GetValue("length")));
                lstItem.SubItems.Add(Utility.GetBsonSize(docFile.GetValue("chunkSize")));
                lstItem.SubItems.Add(ConvertToString(docFile.GetValue("uploadDate")));
                lstItem.SubItems.Add(ConvertToString(docFile.GetValue("md5")));
                //!MONO
                lstItem.SubItems.Add(GetSystemIcon.GetContentType(Filename));
                lstData.Items.Add(lstItem);
            }
            //自动调节列宽
            Common.Logic.Utility.ListViewColumnResize(lstData);
            // 用新的排序方法对ListView排序
            var _lvwGFSColumnSorter = new FillMongoDB.lvwColumnSorter();
            lstData.ListViewItemSorter = _lvwGFSColumnSorter;
            lstData.ColumnClick += (sender, e) =>
            {
                switch (e.Column)
                {
                    case 1:
                    case 2:
                        _lvwGFSColumnSorter.CompareMethod = FillMongoDB.lvwColumnSorter.SortMethod.SizeCompare;
                        break;
                    default:
                        _lvwGFSColumnSorter.CompareMethod = FillMongoDB.lvwColumnSorter.SortMethod.StringCompare;
                        break;
                }
                // 检查点击的列是不是现在的排序列.
                if (e.Column == _lvwGFSColumnSorter.SortColumn)
                {
                    // 重新设置此列的排序方法.
                    _lvwGFSColumnSorter.Order = _lvwGFSColumnSorter.Order == SortOrder.Ascending
                        ? SortOrder.Descending
                        : SortOrder.Ascending;
                }
                else
                {
                    // 设置排序列，默认为正向排序
                    _lvwGFSColumnSorter.SortColumn = e.Column;
                    _lvwGFSColumnSorter.Order = SortOrder.Ascending;
                }
                lstData.Sort();
            };
        }

        #endregion

        #region"数据导航"

        /// <summary>
        ///     数据导航
        /// </summary>
        public enum PageChangeOpr
        {
            /// <summary>
            ///     第一页
            /// </summary>
            FirstPage,

            /// <summary>
            ///     最后一页
            /// </summary>
            LastPage,

            /// <summary>
            ///     上一页
            /// </summary>
            PrePage,

            /// <summary>
            ///     下一页
            /// </summary>
            NextPage
        }

        /// <summary>
        ///     换页操作
        /// </summary>
        /// <param name="pageChangeMode"></param>
        /// <param name="mDataViewInfo"></param>
        /// <param name="dataShower"></param>
        public static void PageChanged(PageChangeOpr pageChangeMode, ref DataViewInfo mDataViewInfo,
            List<Control> dataShower)
        {
            switch (pageChangeMode)
            {
                case PageChangeOpr.FirstPage:
                    mDataViewInfo.SkipCnt = 0;
                    break;
                case PageChangeOpr.LastPage:
                    if (mDataViewInfo.CurrentCollectionTotalCnt%mDataViewInfo.LimitCnt == 0)
                    {
                        //没有余数的时候，600 % 100 == 0  => Skip = 600-100 = 500
                        mDataViewInfo.SkipCnt = mDataViewInfo.CurrentCollectionTotalCnt - mDataViewInfo.LimitCnt;
                    }
                    else
                    {
                        // 630 % 100 == 30  => Skip = 630-30 = 600  
                        mDataViewInfo.SkipCnt = mDataViewInfo.CurrentCollectionTotalCnt -
                                                mDataViewInfo.CurrentCollectionTotalCnt%mDataViewInfo.LimitCnt;
                    }
                    break;
                case PageChangeOpr.NextPage:
                    mDataViewInfo.SkipCnt += mDataViewInfo.LimitCnt;
                    if (mDataViewInfo.SkipCnt >= mDataViewInfo.CurrentCollectionTotalCnt)
                    {
                        mDataViewInfo.SkipCnt = mDataViewInfo.CurrentCollectionTotalCnt - 1;
                    }
                    break;
                case PageChangeOpr.PrePage:
                    mDataViewInfo.SkipCnt -= mDataViewInfo.LimitCnt;
                    if (mDataViewInfo.SkipCnt < 0)
                    {
                        mDataViewInfo.SkipCnt = 0;
                    }
                    break;
                default:
                    break;
            }
            var datalist = DataViewInfo.GetDataList(ref mDataViewInfo, RuntimeMongoDBContext.GetCurrentServer());
            FillDataToControl(datalist, dataShower, mDataViewInfo);
        }

        #endregion
    }
}