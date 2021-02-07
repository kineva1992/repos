// =============================================================
// BytesRoad.NetSuit : A free network library for .NET platform 
// =============================================================
//
// Copyright (C) 2004-2005 BytesRoad Software
// 
// Project Info: http://www.bytesroad.com/NetSuit/
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//========================================================================== 
//  Sockets Sample
//
//  File:		SocketsHttpGet.cs
//  Summary:	Main form for BytesRoad.NetSuit Library HTTP GET sample.
//
//========================================================================== 

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

using System.Text;

using BytesRoad.Net.Sockets;
using BytesRoad.Diag;

namespace SocketsHttpGet
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class HttpGetForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.Button btnExit;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtRes;
		private System.Windows.Forms.TextBox txtURL;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnRequest;
		private System.Windows.Forms.GroupBox groupProxy;
		private System.Windows.Forms.RadioButton rdNone;
		private System.Windows.Forms.RadioButton rdSocks4;
		private System.Windows.Forms.RadioButton rdSocks4a;
		private System.Windows.Forms.RadioButton rdSocks5;
		private System.Windows.Forms.RadioButton rdWeb;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox txtServer;
		private System.Windows.Forms.TextBox txtUser;
		private System.Windows.Forms.TextBox txtPort;
		private System.Windows.Forms.TextBox txtPassword;
		private System.Windows.Forms.CheckBox checkPreauthenticate;
		private System.Windows.Forms.Button btnApplay;
		private System.Windows.Forms.TabPage proxySettingTab;
		private System.Windows.Forms.Button btnClear;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public HttpGetForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(HttpGetForm));
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.btnExit = new System.Windows.Forms.Button();
			this.btnClear = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.txtRes = new System.Windows.Forms.TextBox();
			this.txtURL = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.btnRequest = new System.Windows.Forms.Button();
			this.proxySettingTab = new System.Windows.Forms.TabPage();
			this.btnApplay = new System.Windows.Forms.Button();
			this.checkPreauthenticate = new System.Windows.Forms.CheckBox();
			this.txtPort = new System.Windows.Forms.TextBox();
			this.txtUser = new System.Windows.Forms.TextBox();
			this.txtPassword = new System.Windows.Forms.TextBox();
			this.txtServer = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.groupProxy = new System.Windows.Forms.GroupBox();
			this.rdWeb = new System.Windows.Forms.RadioButton();
			this.rdSocks5 = new System.Windows.Forms.RadioButton();
			this.rdSocks4a = new System.Windows.Forms.RadioButton();
			this.rdSocks4 = new System.Windows.Forms.RadioButton();
			this.rdNone = new System.Windows.Forms.RadioButton();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.proxySettingTab.SuspendLayout();
			this.groupProxy.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.proxySettingTab);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(492, 266);
			this.tabControl1.TabIndex = 0;
			this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.btnExit);
			this.tabPage1.Controls.Add(this.btnClear);
			this.tabPage1.Controls.Add(this.label2);
			this.tabPage1.Controls.Add(this.txtRes);
			this.tabPage1.Controls.Add(this.txtURL);
			this.tabPage1.Controls.Add(this.label1);
			this.tabPage1.Controls.Add(this.btnRequest);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Size = new System.Drawing.Size(484, 240);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Request";
			// 
			// btnExit
			// 
			this.btnExit.Location = new System.Drawing.Point(400, 200);
			this.btnExit.Name = "btnExit";
			this.btnExit.TabIndex = 13;
			this.btnExit.Text = "Exit";
			this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
			// 
			// btnClear
			// 
			this.btnClear.Location = new System.Drawing.Point(400, 50);
			this.btnClear.Name = "btnClear";
			this.btnClear.TabIndex = 12;
			this.btnClear.Text = "Clear";
			this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(11, 42);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(40, 16);
			this.label2.TabIndex = 11;
			this.label2.Text = "Result:";
			// 
			// txtRes
			// 
			this.txtRes.Location = new System.Drawing.Point(11, 64);
			this.txtRes.Multiline = true;
			this.txtRes.Name = "txtRes";
			this.txtRes.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtRes.Size = new System.Drawing.Size(373, 168);
			this.txtRes.TabIndex = 10;
			this.txtRes.Text = "";
			// 
			// txtURL
			// 
			this.txtURL.Location = new System.Drawing.Point(11, 18);
			this.txtURL.Name = "txtURL";
			this.txtURL.Size = new System.Drawing.Size(373, 20);
			this.txtURL.TabIndex = 9;
			this.txtURL.Text = "http://www.bytesroad.com/showenv.aspx";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(11, 2);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(32, 16);
			this.label1.TabIndex = 8;
			this.label1.Text = "URL:";
			// 
			// btnRequest
			// 
			this.btnRequest.Location = new System.Drawing.Point(400, 18);
			this.btnRequest.Name = "btnRequest";
			this.btnRequest.Size = new System.Drawing.Size(75, 24);
			this.btnRequest.TabIndex = 7;
			this.btnRequest.Text = "Request";
			this.btnRequest.Click += new System.EventHandler(this.btnRequest_Click);
			// 
			// proxySettingTab
			// 
			this.proxySettingTab.Controls.Add(this.btnApplay);
			this.proxySettingTab.Controls.Add(this.checkPreauthenticate);
			this.proxySettingTab.Controls.Add(this.txtPort);
			this.proxySettingTab.Controls.Add(this.txtUser);
			this.proxySettingTab.Controls.Add(this.txtPassword);
			this.proxySettingTab.Controls.Add(this.txtServer);
			this.proxySettingTab.Controls.Add(this.label6);
			this.proxySettingTab.Controls.Add(this.label5);
			this.proxySettingTab.Controls.Add(this.label4);
			this.proxySettingTab.Controls.Add(this.label3);
			this.proxySettingTab.Controls.Add(this.groupProxy);
			this.proxySettingTab.Location = new System.Drawing.Point(4, 22);
			this.proxySettingTab.Name = "proxySettingTab";
			this.proxySettingTab.Size = new System.Drawing.Size(484, 240);
			this.proxySettingTab.TabIndex = 1;
			this.proxySettingTab.Text = "Proxy settings";
			// 
			// btnApplay
			// 
			this.btnApplay.Enabled = false;
			this.btnApplay.Location = new System.Drawing.Point(392, 208);
			this.btnApplay.Name = "btnApplay";
			this.btnApplay.TabIndex = 10;
			this.btnApplay.Text = "Apply";
			this.btnApplay.Click += new System.EventHandler(this.btnApplay_Click);
			// 
			// checkPreauthenticate
			// 
			this.checkPreauthenticate.Location = new System.Drawing.Point(80, 128);
			this.checkPreauthenticate.Name = "checkPreauthenticate";
			this.checkPreauthenticate.TabIndex = 9;
			this.checkPreauthenticate.Text = "Preauthenticate";
			this.checkPreauthenticate.CheckedChanged += new System.EventHandler(this.checkPreauthenticate_CheckedChanged);
			// 
			// txtPort
			// 
			this.txtPort.Location = new System.Drawing.Point(80, 48);
			this.txtPort.Name = "txtPort";
			this.txtPort.Size = new System.Drawing.Size(208, 20);
			this.txtPort.TabIndex = 8;
			this.txtPort.Text = "";
			this.txtPort.TextChanged += new System.EventHandler(this.txtPort_TextChanged);
			// 
			// txtUser
			// 
			this.txtUser.Location = new System.Drawing.Point(80, 72);
			this.txtUser.Name = "txtUser";
			this.txtUser.Size = new System.Drawing.Size(208, 20);
			this.txtUser.TabIndex = 7;
			this.txtUser.Text = "";
			this.txtUser.TextChanged += new System.EventHandler(this.txtUser_TextChanged);
			// 
			// txtPassword
			// 
			this.txtPassword.Location = new System.Drawing.Point(80, 96);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.Size = new System.Drawing.Size(208, 20);
			this.txtPassword.TabIndex = 6;
			this.txtPassword.Text = "";
			this.txtPassword.TextChanged += new System.EventHandler(this.txtPassword_TextChanged);
			// 
			// txtServer
			// 
			this.txtServer.Location = new System.Drawing.Point(80, 24);
			this.txtServer.Name = "txtServer";
			this.txtServer.Size = new System.Drawing.Size(208, 20);
			this.txtServer.TabIndex = 5;
			this.txtServer.Text = "";
			this.txtServer.TextChanged += new System.EventHandler(this.txtServer_TextChanged);
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(16, 99);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(64, 16);
			this.label6.TabIndex = 4;
			this.label6.Text = "Password:";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(16, 52);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(32, 16);
			this.label5.TabIndex = 3;
			this.label5.Text = "Port:";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(16, 26);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(48, 16);
			this.label4.TabIndex = 2;
			this.label4.Text = "Server:";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(16, 74);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(32, 16);
			this.label3.TabIndex = 1;
			this.label3.Text = "User:";
			// 
			// groupProxy
			// 
			this.groupProxy.Controls.Add(this.rdWeb);
			this.groupProxy.Controls.Add(this.rdSocks5);
			this.groupProxy.Controls.Add(this.rdSocks4a);
			this.groupProxy.Controls.Add(this.rdSocks4);
			this.groupProxy.Controls.Add(this.rdNone);
			this.groupProxy.Location = new System.Drawing.Point(304, 18);
			this.groupProxy.Name = "groupProxy";
			this.groupProxy.Size = new System.Drawing.Size(128, 142);
			this.groupProxy.TabIndex = 0;
			this.groupProxy.TabStop = false;
			this.groupProxy.Text = "Proxy type";
			// 
			// rdWeb
			// 
			this.rdWeb.Location = new System.Drawing.Point(16, 112);
			this.rdWeb.Name = "rdWeb";
			this.rdWeb.TabIndex = 4;
			this.rdWeb.Text = "Web proxy";
			this.rdWeb.CheckedChanged += new System.EventHandler(this.rdWeb_CheckedChanged);
			// 
			// rdSocks5
			// 
			this.rdSocks5.Location = new System.Drawing.Point(16, 88);
			this.rdSocks5.Name = "rdSocks5";
			this.rdSocks5.TabIndex = 3;
			this.rdSocks5.Text = "Socks5";
			this.rdSocks5.CheckedChanged += new System.EventHandler(this.rdSocks5_CheckedChanged);
			// 
			// rdSocks4a
			// 
			this.rdSocks4a.Location = new System.Drawing.Point(16, 64);
			this.rdSocks4a.Name = "rdSocks4a";
			this.rdSocks4a.TabIndex = 2;
			this.rdSocks4a.Text = "Socks4a";
			this.rdSocks4a.CheckedChanged += new System.EventHandler(this.rdSocks4a_CheckedChanged);
			// 
			// rdSocks4
			// 
			this.rdSocks4.Location = new System.Drawing.Point(16, 40);
			this.rdSocks4.Name = "rdSocks4";
			this.rdSocks4.TabIndex = 1;
			this.rdSocks4.Text = "Socks4";
			this.rdSocks4.CheckedChanged += new System.EventHandler(this.rdSocks4_CheckedChanged);
			// 
			// rdNone
			// 
			this.rdNone.Checked = true;
			this.rdNone.Location = new System.Drawing.Point(16, 16);
			this.rdNone.Name = "rdNone";
			this.rdNone.TabIndex = 0;
			this.rdNone.TabStop = true;
			this.rdNone.Text = "None";
			this.rdNone.CheckedChanged += new System.EventHandler(this.rdNone_CheckedChanged);
			// 
			// HttpGetForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(492, 266);
			this.Controls.Add(this.tabControl1);
			//this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "HttpGetForm";
			this.Text = "BytesRoad.Net.Sockets Sample - HTTP GET Request";
			this.Load += new System.EventHandler(this.HttpGetForm_Load);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.proxySettingTab.ResumeLayout(false);
			this.groupProxy.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new HttpGetForm());
		}

		private void btnRequest_Click(object sender, System.EventArgs e)
		{
			SocketEx sock = null;
			try
			{
				Uri reqUri = new Uri(txtURL.Text);

				string host = reqUri.Host;
				int port = reqUri.Port;
				string path = reqUri.PathAndQuery;

				sock = new SocketEx(_proxyType, _proxyServer, _proxyPort, 
					_proxyUser, _proxyPwd);
				
				//configure preauthenticate
				sock.PreAuthenticate = _preAuthenticate;


				sock.Connect(host, port);
				string cmd = "GET " + path + " HTTP/1.0\r\n" +
							"Host: " + host + "\r\n\r\n";
				sock.Send(_usedEnc.GetBytes(cmd));


				//simple reading loop
				//read while have the data
				try
				{
					byte[] data = new byte[32*1024];
					while(true)
					{
						int dataLen = sock.Receive(data);
						if(0 == dataLen)
							break;
						txtRes.Text += _usedEnc.GetString(data, 0, dataLen);
					}
				}
				catch(Exception ex)
				{
					txtRes.Text += Environment.NewLine + ex.ToString();
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message, "Exception caught!");
			}

			if(null != sock)
				sock.Close();
		}

		private void txtPassword_TextChanged(object sender, System.EventArgs e)
		{
			btnApplay.Enabled = true;
		}

		private void txtPort_TextChanged(object sender, System.EventArgs e)
		{
			btnApplay.Enabled = true;
		}

		private void txtServer_TextChanged(object sender, System.EventArgs e)
		{
			btnApplay.Enabled = true;
		}

		private void txtUser_TextChanged(object sender, System.EventArgs e)
		{
			btnApplay.Enabled = true;
		}

		private void rdNone_CheckedChanged(object sender, System.EventArgs e)
		{
			btnApplay.Enabled = true;
		}

		private void rdSocks4_CheckedChanged(object sender, System.EventArgs e)
		{
			btnApplay.Enabled = true;		
		}

		private void rdSocks4a_CheckedChanged(object sender, System.EventArgs e)
		{
			btnApplay.Enabled = true;		
		}

		private void rdSocks5_CheckedChanged(object sender, System.EventArgs e)
		{
			btnApplay.Enabled = true;		
		}

		private void rdWeb_CheckedChanged(object sender, System.EventArgs e)
		{
			btnApplay.Enabled = true;		
		}

		private void checkPreauthenticate_CheckedChanged(object sender, System.EventArgs e)
		{
			btnApplay.Enabled = true;
		}

		private void tabControl1_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			btnApplay.Enabled = false;

			RestoreProxySettings();
		}

		private void btnApplay_Click(object sender, System.EventArgs e)
		{
			SaveProxySettings();
			btnApplay.Enabled = false;
		}

		Encoding _usedEnc = Encoding.ASCII;

		ProxyType _proxyType = ProxyType.None;
		string _proxyServer = "";
		int _proxyPort = 0;
		byte[] _proxyPwd = null;
		byte[] _proxyUser = null;
		bool _preAuthenticate = false;

		void RestoreProxySettings()
		{
			//restore proxy type
			switch(_proxyType)
			{
			case ProxyType.None: rdNone.Checked = true; break;
			case ProxyType.Socks4: rdSocks4.Checked = true; break;
			case ProxyType.Socks4a: rdSocks4a.Checked = true; break;
			case ProxyType.Socks5: rdSocks5.Checked = true; break;
			case ProxyType.HttpConnect: rdWeb.Checked = true; break;
			}

			//restore proxy server
			txtServer.Text = _proxyServer;

			//restore proxy user
			if(null == _proxyUser)
				txtUser.Text = "";
			else
				txtUser.Text = _usedEnc.GetString(_proxyUser);

			//restore proxy password
			if(null == _proxyPwd)
				txtPassword.Text = "";
			else
				txtPassword.Text = _usedEnc.GetString(_proxyPwd);

			//restore proxy port
			if(0 == _proxyPort)
				txtPort.Text = "";
			else
				txtPort.Text = Convert.ToString(_proxyPort, 10);

			checkPreauthenticate.Checked = _preAuthenticate;
		}

		void SaveProxySettings()
		{
			//save proxy type
			if(rdNone.Checked)
				_proxyType = ProxyType.None;
			else if(rdSocks4.Checked)
				_proxyType = ProxyType.Socks4;
			else if(rdSocks4a.Checked)
				_proxyType = ProxyType.Socks4a;
			else if(rdSocks5.Checked)
				_proxyType = ProxyType.Socks5;
			else if(rdWeb.Checked)
				_proxyType = ProxyType.HttpConnect;
			else
				MessageBox.Show("Proxy type is not selected.", "Error");

			//save proxy server
			_proxyServer = txtServer.Text;

			//save proxy user
			string dummyStr = txtUser.Text.TrimEnd('\0');
			if((null != dummyStr) && (dummyStr.Length > 0))
				_proxyUser = _usedEnc.GetBytes(dummyStr);
			else
				_proxyUser = null;

			//save proxy password
			dummyStr = txtPassword.Text.TrimEnd('\0');
			if((null != dummyStr) && (dummyStr.Length > 0))
				_proxyPwd = _usedEnc.GetBytes(dummyStr);
			else
				_proxyPwd = null;

			//save proxy port
			dummyStr = txtPort.Text.TrimEnd('\0');
			if((null != dummyStr) && (dummyStr.Length > 0))
				_proxyPort = Convert.ToInt32(dummyStr, 10);
			else
				_proxyPort = 0;

			_preAuthenticate = checkPreauthenticate.Checked;
		}

		private void HttpGetForm_Load(object sender, System.EventArgs e)
		{
		
		}

		private void btnExit_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void btnClear_Click(object sender, System.EventArgs e)
		{
			txtRes.Clear();
		}
	}
}
