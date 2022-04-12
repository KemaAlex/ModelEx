﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelEx
{
	public partial class MainWindow : Form
	{
		ProgressWindow _ProgressWindow;
		CDC.Objects.ExportOptions _ImportExportOptions;
		int _MainSplitPanelPosition;
		protected bool _RunUIMonitoringThread;
		protected bool _ReloadModelOnRenderModeChange;
		protected bool _ResetCameraOnModelLoad;
		protected bool _ClearLoadedResources;
		protected bool _SelectObjectOnLoaded;
		protected bool _SelectSceneOnLoaded;
		protected string _LastExportDirectory = "";
		protected RenderManager.LoadRequestCDC _LoadRequest;

		int _SceneMode;
		public int SceneMode
		{
			get { return _SceneMode; }
			set
			{
				_SceneMode = value;

				if (_SceneMode == 0)
				{
					RenderManager.Instance.SceneMode = ViewMode.Scene;
					sceneControls.Visible = true;
					objectControls.Visible = false;
					sceneToolStripMenuItem.Checked = true;
					objectToolStripMenuItem.Checked = false;
				}
				else if (_SceneMode == 1)
				{
					RenderManager.Instance.SceneMode = ViewMode.Object;
					sceneControls.Visible = false;
					objectControls.Visible = true;
					sceneToolStripMenuItem.Checked = false;
					objectToolStripMenuItem.Checked = true;
				}
				else
				{
					RenderManager.Instance.SceneMode = ViewMode.None;
					sceneControls.Visible = false;
					objectControls.Visible = false;
					sceneToolStripMenuItem.Checked = false;
					objectToolStripMenuItem.Checked = false;
				}
			}
		}

		protected void UIMonitor()
		{
			while (_RunUIMonitoringThread)
			{
				UpdateSplitPanelPosition();
				Thread.Sleep(1000);
			}
		}

		protected void UpdateSplitPanelPosition()
		{
			_MainSplitPanelPosition = sceneViewContainer.SplitterDistance;
			//Console.WriteLine(string.Format("Splitter position is now {0}", _MainSplitPanelPosition));
		}

		protected void ResetSplitPanelPosition()
		{
			sceneViewContainer.SplitterDistance = _MainSplitPanelPosition;
			//Console.WriteLine(string.Format("Reset splitter position to {0}", _MainSplitPanelPosition));
		}

		public MainWindow()
		{
			_RunUIMonitoringThread = true;
			InitializeComponent();
			SceneMode = 0;
			_ImportExportOptions = new CDC.Objects.ExportOptions();
			UpdateSplitPanelPosition();
			ThreadStart tsUIMonitor = new ThreadStart(UIMonitor);
			Thread uiMonitor = new Thread(tsUIMonitor);
			uiMonitor.Start();
		}

		private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
		{
			_RunUIMonitoringThread = false;
			sceneView.ShutDown();
		}

		private void MainWindow_Load(object sender, EventArgs e)
		{
			sceneView.Initialize();
			_ReloadModelOnRenderModeChange = reloadModelWhenRenderModeIsChangedToolStripMenuItem.Checked;
			_ResetCameraOnModelLoad = resetCameraPositionWhenModelIsLoadedToolStripMenuItem.Checked;
			ResetPlatformDetection();
			autodetectToolStripMenuItem.Checked = true;
			_ImportExportOptions.ForcedPlatform = CDC.Platform.None;
		}

		protected void BeginLoading()
		{
			Enabled = false;
			_ProgressWindow?.Dispose();
			_ProgressWindow = new ProgressWindow();
			_ProgressWindow.Title = "Loading";
			_ProgressWindow.SetMessage("");
			//progressWindow.Icon = this.Icon;
			_ProgressWindow.Owner = this;
			_ProgressWindow.TopLevel = true;
			_ProgressWindow.ShowInTaskbar = false;
			_ProgressWindow.Show();

			if (_ClearLoadedResources)
			{
				objectControls.ResourceCombo.SelectedIndex = -1;
				sceneControls.ResourceCombo.SelectedIndex = -1;

				resourceList.Items.Clear();
				objectControls.ResourceCombo.Items.Clear();
				sceneControls.ResourceCombo.Items.Clear();
			}
		}

		protected void EndLoading()
		{
			foreach (string resourceName in RenderManager.Instance.Resources.Keys)
			{
				if (resourceName != "")
				{
					if (!resourceList.Items.ContainsKey(resourceName))
					{
						resourceList.Items.Add(resourceName, resourceName, -1);
					}

					if (!objectControls.ResourceCombo.Items.Contains(resourceName))
					{
						objectControls.ResourceCombo.Items.Add(resourceName);
					}

					if (!sceneControls.ResourceCombo.Items.Contains(resourceName))
					{
						// Maybe filter for only levels here?
						sceneControls.ResourceCombo.Items.Add(resourceName);
					}
				}
			}

			if (_SelectObjectOnLoaded && objectControls.ResourceCombo.Items.Contains(_LoadRequest.ResourceName))
			{
				objectControls.ResourceCombo.SelectedIndex = objectControls.ResourceCombo.Items.IndexOf(_LoadRequest.ResourceName);
				optionTabs.SelectedIndex = 1;
				SceneMode = 1;
				CameraManager.Instance.Reset();
			}

			if (_SelectSceneOnLoaded && sceneControls.ResourceCombo.Items.Contains(_LoadRequest.ResourceName))
			{
				sceneControls.ResourceCombo.SelectedIndex = sceneControls.ResourceCombo.Items.IndexOf(_LoadRequest.ResourceName);
				optionTabs.SelectedIndex = 1;
				SceneMode = 0;
				CameraManager.Instance.Reset();
			}

			_SelectObjectOnLoaded = false;
			_SelectSceneOnLoaded = false;
			_ClearLoadedResources = false;

			if (_ResetCameraOnModelLoad)
			{
				CameraManager.Instance.Reset();
			}

			Enabled = true;
			_ProgressWindow.Hide();
			_ProgressWindow.Dispose();
		}

		protected bool SelectResourceToLoad(bool selectObjectOnLoaded, bool selectSceneOnLoaded)
		{
			_LoadRequest = new RenderManager.LoadRequestCDC();
			LoadResourceDialog loadResourceDialog = new LoadResourceDialog();

			if (Properties.Settings.Default.RecentFolder != null &&
				Directory.Exists(Properties.Settings.Default.RecentFolder))
			{
				loadResourceDialog.SelectedFolder = Properties.Settings.Default.RecentFolder;
			}

			loadResourceDialog.SelectedGameType = (CDC.Game)Properties.Settings.Default.RecentGame;
			loadResourceDialog.SelectedPlatform = (CDC.Platform)Properties.Settings.Default.RecentPlatform;

			if (selectObjectOnLoaded)
			{
				loadResourceDialog.Text = "Load Object...";
			}
			else if (selectSceneOnLoaded)
			{
				loadResourceDialog.Text = "Load Scene...";
			}
			else
			{
				loadResourceDialog.Text = "Load Resource...";
			}

			if (loadResourceDialog.ShowDialog() != DialogResult.OK)
			{
				Properties.Settings.Default.RecentFolder = loadResourceDialog.SelectedFolder;
				Properties.Settings.Default.RecentGame = (int)loadResourceDialog.SelectedGameType;
				Properties.Settings.Default.RecentPlatform = (int)loadResourceDialog.SelectedPlatform;
				Properties.Settings.Default.Save();
				loadResourceDialog.Dispose();
				return false;
			}

			loadResourceDialog.Dispose();

			if (loadResourceDialog.SelectedGameType == CDC.Game.Gex)
			{
				CDC.Objects.GexFile gexFile;
				ObjectSelectWindow objectSelectDlg = new ObjectSelectWindow();

				try
				{
					gexFile = new CDC.Objects.GexFile(loadResourceDialog.DataFile, _ImportExportOptions);
					if (gexFile.Asset == CDC.Asset.Unit)
					{
						objectSelectDlg.SetObjectNames(gexFile.ObjectNames);
						if (objectSelectDlg.ShowDialog() != DialogResult.OK)
						{
							objectSelectDlg.Dispose();
							return false;
						}

						objectSelectDlg.Dispose();
					}
				}
				catch
				{
					return false;
				}

				_LoadRequest.ChildIndex = objectSelectDlg.SelectedObject;
			}

			_LoadRequest.DataFile = loadResourceDialog.DataFile;
			_LoadRequest.TextureFile = loadResourceDialog.TextureFile;
			_LoadRequest.ObjectListFile = loadResourceDialog.ObjectListFile;
			_LoadRequest.GameType = loadResourceDialog.SelectedGameType;
			_LoadRequest.ExportOptions = _ImportExportOptions;

			_ClearLoadedResources = loadResourceDialog.ClearLoadedFiles;
			_SelectObjectOnLoaded = selectObjectOnLoaded;
			_SelectSceneOnLoaded = selectSceneOnLoaded;

			Properties.Settings.Default.RecentFolder = loadResourceDialog.SelectedFolder;
			Properties.Settings.Default.RecentGame = (int)loadResourceDialog.SelectedGameType;
			Properties.Settings.Default.RecentPlatform = (int)loadResourceDialog.SelectedPlatform;
			Properties.Settings.Default.Save();

			reloadCurrentModelToolStripMenuItem.Enabled = true;

			return true;
		}

		protected void LoadResource()
		{
			if (!File.Exists(_LoadRequest.DataFile))
			{
				return;
			}

			Thread loadingThread = new Thread((() =>
			{
				Invoke(new MethodInvoker(BeginLoading));

				if (_ClearLoadedResources)
				{
					RenderManager.Instance.UnloadResources();
				}

				RenderManager.Instance.LoadResourceCDC(_LoadRequest);

				Invoke(new MethodInvoker(EndLoading));
			}));

			loadingThread.Name = "LoadingThread";
			loadingThread.SetApartmentState(ApartmentState.STA);
			loadingThread.Start();
			//loadingThread.Join();

			Thread progressThread = new Thread(() =>
			{
				do
				{
					if (_ProgressWindow != null)
					{
						lock (SceneCDC.ProgressStage)
						{
							_ProgressWindow.SetMessage(SceneCDC.ProgressStage);
						}

						int oldProgress = _ProgressWindow.GetProgress();
						if (oldProgress < SceneCDC.ProgressPercent)
						{
							_ProgressWindow.SetProgress(oldProgress + 1);
						}
					}

					Thread.Sleep(20);
				}
				while (loadingThread.IsAlive);
			});

			progressThread.Name = "ProgressThread";
			progressThread.SetApartmentState(ApartmentState.STA);
			progressThread.Start();
		}

		private void exportObjectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveFileDialog SaveDlg = new SaveFileDialog
			{
				CheckPathExists = true,
				Filter =
					"Collada Mesh Files (*.dae)|*.dae",
				//"All Files (*.*)|*.*";
				DefaultExt = "dae",
				FilterIndex = 1
			};

			if (Directory.Exists(_LastExportDirectory))
			{
				SaveDlg.InitialDirectory = _LastExportDirectory;
			}

			if (SaveDlg.ShowDialog() == DialogResult.OK)
			{
				_LastExportDirectory = Path.GetDirectoryName(SaveDlg.FileName);
				RenderManager.Instance.ExportCurrentObject(SaveDlg.FileName, _ImportExportOptions);
			}
		}

		private void exportSceneToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveFileDialog SaveDlg = new SaveFileDialog
			{
				CheckPathExists = true,
				Filter =
					"Collada Mesh Files (*.dae)|*.dae",
				//"All Files (*.*)|*.*";
				DefaultExt = "dae",
				FilterIndex = 1
			};

			if (Directory.Exists(_LastExportDirectory))
			{
				SaveDlg.InitialDirectory = _LastExportDirectory;
			}

			if (SaveDlg.ShowDialog() == DialogResult.OK)
			{
				_LastExportDirectory = Path.GetDirectoryName(SaveDlg.FileName);
				RenderManager.Instance.ExportCurrentScene(SaveDlg.FileName, _ImportExportOptions);
			}
		}

		private void sceneTree_AfterCheck(object sender, TreeViewEventArgs e)
		{
			Scene currentScene = (Scene)RenderManager.Instance.CurrentScene;
			if (currentScene != null)
			{
				foreach (Renderable renderable in currentScene.RenderInstances)
				{
					if (renderable is RenderInstance)
					{
						if (renderable.Name == e.Node.Text)
						{
							((RenderInstance)renderable).Root.Visible = e.Node.Checked;
							continue;
						}

						VisibilityNode node = ((RenderInstance)renderable).FindNode(e.Node.Text);
						if (node != null)
						{
							node.Visible = e.Node.Checked;
						}
					}
				}
			}
		}

		private void objectTree_AfterCheck(object sender, TreeViewEventArgs e)
		{
			Scene currentObject = (Scene)RenderManager.Instance.CurrentObject;
			if (currentObject != null)
			{
				foreach (Renderable renderable in currentObject.RenderInstances)
				{
					if (renderable is RenderInstance)
					{
						if (renderable.Name == e.Node.Text)
						{
							((RenderInstance)renderable).Root.Visible = e.Node.Checked;
							continue;
						}

						VisibilityNode node = ((RenderInstance)renderable).FindNode(e.Node.Text);
						if (node != null)
						{
							node.Visible = e.Node.Checked;
						}
					}
				}
			}
		}

		private void ResetPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CameraManager.Instance.Reset();
		}

		private void EgoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CameraManager.Instance.SetCamera(CameraManager.CameraMode.Ego);
			egoToolStripMenuItem.Checked = true;
			orbitToolStripMenuItem.Checked = false;
			orbitPanToolStripMenuItem.Checked = false;
		}

		private void OrbitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CameraManager.Instance.SetCamera(CameraManager.CameraMode.Orbit);
			egoToolStripMenuItem.Checked = false;
			orbitToolStripMenuItem.Checked = true;
			orbitPanToolStripMenuItem.Checked = false;
		}

		private void OrbitPanToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CameraManager.Instance.SetCamera(CameraManager.CameraMode.OrbitPan);
			egoToolStripMenuItem.Checked = false;
			orbitToolStripMenuItem.Checked = false;
			orbitPanToolStripMenuItem.Checked = true;
		}

		protected void resetRenderModeMenu()
		{
			standardToolStripMenuItem.Checked = false;
			wireframeToolStripMenuItem.Checked = false;
			noTexturemapsToolStripMenuItem.Checked = false;
			debugPolygonFlags1ToolStripMenuItem.Checked = false;
			debugPolygonFlags2ToolStripMenuItem.Checked = false;
			debugPolygonFlags3ToolStripMenuItem.Checked = false;
			debugPolygonFlagsSoulReaverAToolStripMenuItem.Checked = false;
			debugPolygonFlagsHashToolStripMenuItem.Checked = false;
			debugTextureAttributes1ToolStripMenuItem.Checked = false;
			debugTextureAttributes2ToolStripMenuItem.Checked = false;
			debugTextureAttributes3ToolStripMenuItem.Checked = false;
			debugTextureAttributes4ToolStripMenuItem.Checked = false;
			debugTextureAttributes5ToolStripMenuItem.Checked = false;
			debugTextureAttributes6ToolStripMenuItem.Checked = false;
			debugTextureAttributesHashToolStripMenuItem.Checked = false;
			debugTextureAttributesAHashToolStripMenuItem.Checked = false;
			debugTextureAttributesA1ToolStripMenuItem.Checked = false;
			debugTextureAttributesA2ToolStripMenuItem.Checked = false;
			debugTextureAttributesA3ToolStripMenuItem.Checked = false;
			debugTextureAttributesA4ToolStripMenuItem.Checked = false;
			debugTextureAttributesA5ToolStripMenuItem.Checked = false;
			debugTextureAttributesA6ToolStripMenuItem.Checked = false;
			debugTexturePage1ToolStripMenuItem.Checked = false;
			debugTexturePage2ToolStripMenuItem.Checked = false;
			debugTexturePage3ToolStripMenuItem.Checked = false;
			debugTexturePage4ToolStripMenuItem.Checked = false;
			debugTexturePage5ToolStripMenuItem.Checked = false;
			debugTexturePage6ToolStripMenuItem.Checked = false;
			debugTexturePageHashToolStripMenuItem.Checked = false;
			debugTexturePageUpper28BitsHashToolStripMenuItem.Checked = false;
			debugTexturePageUpper5BitsHashToolStripMenuItem.Checked = false;
			rootBSPTreeNumberToolStripMenuItem.Checked = false;
			bSPTreeRootFlagsHashToolStripMenuItem.Checked = false;
			bSPTreeNodeIDToolStripMenuItem.Checked = false;
			bSPTreeRootFlags1ToolStripMenuItem.Checked = false;
			bSPTreeRootFlags2ToolStripMenuItem.Checked = false;
			bSPTreeRootFlags3ToolStripMenuItem.Checked = false;
			bSPTreeRootFlags4ToolStripMenuItem.Checked = false;
			bSPTreeRootFlags5ToolStripMenuItem.Checked = false;
			bSPTreeRootFlags6ToolStripMenuItem.Checked = false;
			bSPTreeNodeFlags1ToolStripMenuItem.Checked = false;
			bSPTreeNodeFlags2ToolStripMenuItem.Checked = false;
			bSPTreeNodeFlags3ToolStripMenuItem.Checked = false;
			bSPTreeNodeFlags4ToolStripMenuItem.Checked = false;
			bSPTreeNodeFlags5ToolStripMenuItem.Checked = false;
			bSPTreeNodeFlags6ToolStripMenuItem.Checked = false;
			bSPTreeParentNodeFlagsORd1ToolStripMenuItem.Checked = false;
			bSPTreeParentNodeFlagsORd2ToolStripMenuItem.Checked = false;
			bSPTreeParentNodeFlagsORd3ToolStripMenuItem.Checked = false;
			bSPTreeParentNodeFlagsORd4ToolStripMenuItem.Checked = false;
			bSPTreeParentNodeFlagsORd5ToolStripMenuItem.Checked = false;
			bSPTreeParentNodeFlagsORd6ToolStripMenuItem.Checked = false;
			bSPTreeParentNodeFlagsORdHashToolStripMenuItem.Checked = false;
			bSPTreeLeafFlags1ToolStripMenuItem.Checked = false;
			bSPTreeLeafFlags2ToolStripMenuItem.Checked = false;
			bSPTreeLeafFlags3ToolStripMenuItem.Checked = false;
			bSPTreeLeafFlags4ToolStripMenuItem.Checked = false;
			bSPTreeLeafFlags5ToolStripMenuItem.Checked = false;
			bSPTreeLeafFlags6ToolStripMenuItem.Checked = false;
			bSPTreeNodeFlagsHashToolStripMenuItem.Checked = false;
			bSPTreeLeafFlagsHashToolStripMenuItem.Checked = false;
			cLUT1ToolStripMenuItem.Checked = false;
			cLUT2ToolStripMenuItem.Checked = false;
			cLUT3ToolStripMenuItem.Checked = false;
			cLUT4ToolStripMenuItem.Checked = false;
			cLUT5ToolStripMenuItem.Checked = false;
			cLUT6ToolStripMenuItem.Checked = false;
			cLUTHashToolStripMenuItem.Checked = false;
			cLUTNonRowColBitsHashToolStripMenuItem.Checked = false;
			cLUTNonRowColBits1ToolStripMenuItem.Checked = false;
			cLUTNonRowColBits2ToolStripMenuItem.Checked = false;
			boneIDHashToolStripMenuItem.Checked = false;
			debugSortPushHashToolStripMenuItem.Checked = false;
			debugSortPushFlags1ToolStripMenuItem.Checked = false;
			debugSortPushFlags2ToolStripMenuItem.Checked = false;
			debugSortPushFlags3ToolStripMenuItem.Checked = false;
			averageVertexAlphaToolStripMenuItem.Checked = false;
			polygonAlphaToolStripMenuItem.Checked = false;
			polygonOpacityToolStripMenuItem.Checked = false;
		}

		protected void HandleRenderModeChange()
		{
			RenderManager.Instance.Wireframe = _ImportExportOptions.RenderMode == CDC.Objects.RenderMode.Wireframe;

			if (_ReloadModelOnRenderModeChange)
			{
				LoadResource();
			}
		}

		private void standardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			standardToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.Standard;
			HandleRenderModeChange();
		}

		private void wireframeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			wireframeToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.Wireframe;
			HandleRenderModeChange();
		}

		private void noTexturemapsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			noTexturemapsToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.NoTextures;
			HandleRenderModeChange();
		}

		private void unhide100InvisibleTexturesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			unhide100InvisibleTexturesToolStripMenuItem.Checked = !unhide100InvisibleTexturesToolStripMenuItem.Checked;
			_ImportExportOptions.UnhideCompletelyTransparentTextures = unhide100InvisibleTexturesToolStripMenuItem.Checked;
		}

		private void discardHiddenPolygonsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			discardHiddenPolygonsToolStripMenuItem.Checked = !discardHiddenPolygonsToolStripMenuItem.Checked;
			_ImportExportOptions.DiscardNonVisible = discardHiddenPolygonsToolStripMenuItem.Checked;
		}

		private void missingPalettesInGreyscaleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			missingPalettesInGreyscaleToolStripMenuItem.Checked = !missingPalettesInGreyscaleToolStripMenuItem.Checked;
			_ImportExportOptions.AlwaysUseGreyscaleForMissingPalettes = missingPalettesInGreyscaleToolStripMenuItem.Checked;
		}

		private void exportDoubleSidedMaterialsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			exportDoubleSidedMaterialsToolStripMenuItem.Checked = !exportDoubleSidedMaterialsToolStripMenuItem.Checked;
			_ImportExportOptions.ExportDoubleSidedMaterials = exportDoubleSidedMaterialsToolStripMenuItem.Checked;
		}

		private void exportSpectralVersionOfAreaFilesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			exportSpectralVersionOfAreaFilesToolStripMenuItem.Checked = !exportSpectralVersionOfAreaFilesToolStripMenuItem.Checked;
			_ImportExportOptions.ExportSpectral = exportSpectralVersionOfAreaFilesToolStripMenuItem.Checked;
		}

		private void makeAllPolygonsVisibleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			makeAllPolygonsVisibleToolStripMenuItem.Checked = !makeAllPolygonsVisibleToolStripMenuItem.Checked;
			_ImportExportOptions.MakeAllPolygonsVisible = makeAllPolygonsVisibleToolStripMenuItem.Checked;
		}

		private void makeAllPolygonsOpaqueToolStripMenuItem_Click(object sender, EventArgs e)
		{
			makeAllPolygonsOpaqueToolStripMenuItem.Checked = !makeAllPolygonsOpaqueToolStripMenuItem.Checked;
			_ImportExportOptions.MakeAllPolygonsOpaque = makeAllPolygonsOpaqueToolStripMenuItem.Checked;
		}

		private void oRAllPolygonColoursWithGreenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			oRAllPolygonColoursWithGreenToolStripMenuItem.Checked = !oRAllPolygonColoursWithGreenToolStripMenuItem.Checked;
			_ImportExportOptions.SetAllPolygonColoursToValue = oRAllPolygonColoursWithGreenToolStripMenuItem.Checked;
		}

		private void includeTreeRootFlagsInORdParentFlagsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			includeTreeRootFlagsInORdParentFlagsToolStripMenuItem.Checked = !includeTreeRootFlagsInORdParentFlagsToolStripMenuItem.Checked;
			_ImportExportOptions.BSPRenderingIncludeRootTreeFlagsWhenORing = includeTreeRootFlagsInORdParentFlagsToolStripMenuItem.Checked;
		}

		private void includeLeafFlagsInORdParentFlagsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			includeLeafFlagsInORdParentFlagsToolStripMenuItem.Checked = !includeLeafFlagsInORdParentFlagsToolStripMenuItem.Checked;
			_ImportExportOptions.BSPRenderingIncludeLeafFlagsWhenORing = includeLeafFlagsInORdParentFlagsToolStripMenuItem.Checked;
		}

		private void MainWindow_Resize(object sender, EventArgs e)
		{
			ResetSplitPanelPosition();
		}

		private void MainWindow_Enter(object sender, EventArgs e)
		{
			UpdateSplitPanelPosition();
		}

		private void MainWindow_Leave(object sender, EventArgs e)
		{
			UpdateSplitPanelPosition();
		}

		private void sceneViewContainer_SplitterMoved(object sender, SplitterEventArgs e)
		{

		}

		private void sceneViewContainer_MouseDown(object sender, MouseEventArgs e)
		{
			UpdateSplitPanelPosition();
		}

		private void sceneViewContainer_MouseClick(object sender, MouseEventArgs e)
		{
			UpdateSplitPanelPosition();
		}

		private void sceneViewContainer_MouseUp(object sender, MouseEventArgs e)
		{
			UpdateSplitPanelPosition();
		}

		private void sceneViewContainer_MouseEnter(object sender, EventArgs e)
		{
			UpdateSplitPanelPosition();
		}

		private void sceneViewContainer_Enter(object sender, EventArgs e)
		{
			UpdateSplitPanelPosition();
		}

		private void sceneViewContainer_Click(object sender, EventArgs e)
		{
			UpdateSplitPanelPosition();
		}

		private void sceneViewContainer_Leave(object sender, EventArgs e)
		{
			UpdateSplitPanelPosition();
		}

		private void sceneViewContainer_SplitterMoving(object sender, SplitterCancelEventArgs e)
		{
			UpdateSplitPanelPosition();
		}

		private void sceneViewContainer_SplitterMoved_1(object sender, SplitterEventArgs e)
		{
			UpdateSplitPanelPosition();
		}



		private void debugPolygonFlags1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugPolygonFlags1ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugPolygonFlags1;
			HandleRenderModeChange();
		}

		private void debugPolygonFlags2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugPolygonFlags2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugPolygonFlags2;
			HandleRenderModeChange();
		}

		private void debugPolygonFlags3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugPolygonFlags3ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugPolygonFlags3;
			HandleRenderModeChange();
		}

		private void debugPolygonFlagsSoulReaverAToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugPolygonFlagsSoulReaverAToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugPolygonFlagsSoulReaverA;
			HandleRenderModeChange();
		}

		private void debugTextureAttributes1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributes1ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributes1;
			HandleRenderModeChange();
		}

		private void debugTextureAttributes2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributes2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributes2;
			HandleRenderModeChange();
		}

		private void debugTextureAttributes3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributes3ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributes3;
			HandleRenderModeChange();
		}

		private void debugTextureAttributes4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributes4ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributes4;
			HandleRenderModeChange();
		}

		private void debugTextureAttributes5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributes5ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributes5;
			HandleRenderModeChange();
		}

		private void debugTextureAttributes6ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributes6ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributes6;
			HandleRenderModeChange();
		}

		private void debugTextureAttributesAHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributesAHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributesAHash;
			HandleRenderModeChange();
		}

		private void debugTextureAttributesA1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributesA1ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributesA1;
			HandleRenderModeChange();
		}

		private void debugTextureAttributesA2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributesA2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributesA2;
			HandleRenderModeChange();
		}

		private void debugTextureAttributesA3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributesA3ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributesA3;
			HandleRenderModeChange();
		}

		private void debugTextureAttributesA4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributesA4ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributesA4;
			HandleRenderModeChange();
		}

		private void debugTextureAttributesA5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributesA5ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributesA5;
			HandleRenderModeChange();
		}

		private void debugTextureAttributesA6ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributesA6ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributesA6;
			HandleRenderModeChange();
		}

		private void debugTexturePage1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTexturePage1ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTexturePage1;
			HandleRenderModeChange();
		}

		private void debugTexturePage2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTexturePage2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTexturePage2;
			HandleRenderModeChange();
		}

		private void debugTexturePage3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTexturePage3ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTexturePage3;
			HandleRenderModeChange();
		}

		private void debugTexturePage4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTexturePage4ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTexturePage4;
			HandleRenderModeChange();
		}

		private void debugTexturePage5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTexturePage5ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTexturePage5;
			HandleRenderModeChange();
		}

		private void debugTexturePage6ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTexturePage6ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTexturePage6;
			HandleRenderModeChange();
		}

		private void rootBSPTreeNumberToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			rootBSPTreeNumberToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPRootTreeNumber;
			HandleRenderModeChange();
		}

		private void bSPTreeNodeIDToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeNodeIDToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeNodeID;
			HandleRenderModeChange();
		}

		private void bSPTreeRootFlags1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeRootFlags1ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPRootTreeFlags1;
			HandleRenderModeChange();
		}

		private void bSPTreeRootFlags2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeRootFlags2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPRootTreeFlags2;
			HandleRenderModeChange();
		}

		private void bSPTreeRootFlags3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeRootFlags3ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPRootTreeFlags3;
			HandleRenderModeChange();
		}

		private void bSPTreeRootFlags4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeRootFlags4ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPRootTreeFlags4;
			HandleRenderModeChange();
		}

		private void bSPTreeRootFlags5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeRootFlags5ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPRootTreeFlags5;
			HandleRenderModeChange();
		}

		private void bSPTreeRootFlags6ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeRootFlags6ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPRootTreeFlags6;
			HandleRenderModeChange();
		}

		private void bSPTreeNodeFlags1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeNodeFlags1ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeImmediateParentFlags1;
			HandleRenderModeChange();
		}

		private void bSPTreeNodeFlags2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeNodeFlags2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeImmediateParentFlags2;
			HandleRenderModeChange();
		}

		private void bSPTreeNodeFlags3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeNodeFlags3ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeImmediateParentFlags3;
			HandleRenderModeChange();
		}

		private void bSPTreeNodeFlags4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeNodeFlags4ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeImmediateParentFlags4;
			HandleRenderModeChange();
		}

		private void bSPTreeNodeFlags5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeNodeFlags5ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeImmediateParentFlags5;
			HandleRenderModeChange();
		}

		private void bSPTreeNodeFlags6ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeNodeFlags6ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeImmediateParentFlags6;
			HandleRenderModeChange();
		}

		private void bSPTreeParentNodeFlagsORd1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeParentNodeFlagsORd1ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeAllParentFlagsORd1;
			HandleRenderModeChange();
		}

		private void bSPTreeParentNodeFlagsORd2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeParentNodeFlagsORd2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeAllParentFlagsORd2;
			HandleRenderModeChange();
		}

		private void bSPTreeParentNodeFlagsORd3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeParentNodeFlagsORd3ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeAllParentFlagsORd3;
			HandleRenderModeChange();
		}

		private void bSPTreeParentNodeFlagsORd4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeParentNodeFlagsORd4ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeAllParentFlagsORd4;
			HandleRenderModeChange();
		}

		private void bSPTreeParentNodeFlagsORd5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeParentNodeFlagsORd5ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeAllParentFlagsORd5;
			HandleRenderModeChange();
		}

		private void bSPTreeParentNodeFlagsORd6ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeParentNodeFlagsORd6ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeAllParentFlagsORd6;
			HandleRenderModeChange();
		}

		private void bSPTreeLeafFlags1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeLeafFlags1ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeLeafFlags1;
			HandleRenderModeChange();
		}

		private void bSPTreeLeafFlags2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeLeafFlags2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeLeafFlags2;
			HandleRenderModeChange();
		}

		private void bSPTreeLeafFlags3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeLeafFlags3ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeLeafFlags3;
			HandleRenderModeChange();
		}

		private void bSPTreeLeafFlags4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeLeafFlags4ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeLeafFlags4;
			HandleRenderModeChange();
		}

		private void bSPTreeLeafFlags5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeLeafFlags5ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeLeafFlags5;
			HandleRenderModeChange();
		}

		private void bSPTreeLeafFlags6ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeLeafFlags6ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeLeafFlags6;
			HandleRenderModeChange();
		}

		private void debugPolygonFlagsHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugPolygonFlagsHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugPolygonFlagsHash;
			HandleRenderModeChange();
		}

		private void debugTextureAttributesHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTextureAttributesHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTextureAttributesHash;
			HandleRenderModeChange();
		}

		private void debugTexturePageHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTexturePageHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTexturePageHash;
			HandleRenderModeChange();
		}

		private void bSPTreeRootFlagsHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeRootFlagsHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPRootTreeFlagsHash;
			HandleRenderModeChange();
		}

		private void bSPTreeNodeFlagsHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeNodeFlagsHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeImmediateParentFlagsHash;
			HandleRenderModeChange();
		}

		private void bSPTreeParentNodeFlagsORdHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeParentNodeFlagsORdHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeAllParentFlagsORdHash;
			HandleRenderModeChange();
		}

		private void bSPTreeLeafFlagsHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			bSPTreeLeafFlagsHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBSPTreeLeafFlagsHash;
			HandleRenderModeChange();
		}

		private void debugTexturePageUpper24BitsHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTexturePageUpper28BitsHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTexturePageUpper28BitsHash;
			HandleRenderModeChange();
		}

		private void debugTexturePageUpper5BitsHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugTexturePageUpper5BitsHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugTexturePageUpper5BitsHash;
			HandleRenderModeChange();
		}

		private void cLUTHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			cLUTHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugCLUTHash;
			HandleRenderModeChange();
		}

		private void cLUT1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			cLUT1ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugCLUT1;
			HandleRenderModeChange();
		}

		private void cLUT2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			cLUT2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugCLUT2;
			HandleRenderModeChange();
		}

		private void cLUT3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			cLUT3ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugCLUT3;
			HandleRenderModeChange();
		}

		private void cLUT4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			cLUT4ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugCLUT4;
			HandleRenderModeChange();
		}

		private void cLUT5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			cLUT5ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugCLUT5;
			HandleRenderModeChange();
		}

		private void cLUT6ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			cLUT6ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugCLUT6;
			HandleRenderModeChange();
		}

		private void cLUTNonRowColBitsHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			cLUTNonRowColBitsHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugCLUTNonRowColBitsHash;
			HandleRenderModeChange();
		}

		private void cLUTNonRowColBits1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			cLUTNonRowColBits1ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugCLUTNonRowColBits1;
			HandleRenderModeChange();
		}

		private void cLUTNonRowColBits2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			cLUTNonRowColBits2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugCLUTNonRowColBits2;
			HandleRenderModeChange();
		}

		private void boneIDHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			boneIDHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugBoneIDHash;
			HandleRenderModeChange();
		}

		private void debugSortPushHashToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugSortPushHashToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugSortPushHash;
			HandleRenderModeChange();
		}

		private void debugSortPushFlags1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugSortPushFlags1ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugSortPushFlags1;
			HandleRenderModeChange();
		}

		private void debugSortPushFlags2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugSortPushFlags2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugSortPushFlags2;
			HandleRenderModeChange();
		}

		private void debugSortPushFlags3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			debugSortPushFlags3ToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.DebugSortPushFlags3;
			HandleRenderModeChange();
		}

		private void averageVertexAlphaToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			averageVertexAlphaToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.AverageVertexAlpha;
			HandleRenderModeChange();
		}

		private void polygonAlphaToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			polygonAlphaToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.PolygonAlpha;
			HandleRenderModeChange();
		}

		private void polygonOpacityToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetRenderModeMenu();
			polygonOpacityToolStripMenuItem.Checked = true;
			_ImportExportOptions.RenderMode = CDC.Objects.RenderMode.PolygonOpacity;
			HandleRenderModeChange();
		}

		private void reloadModelWhenRenderModeIsChangedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			reloadModelWhenRenderModeIsChangedToolStripMenuItem.Checked = !reloadModelWhenRenderModeIsChangedToolStripMenuItem.Checked;
			_ReloadModelOnRenderModeChange = reloadModelWhenRenderModeIsChangedToolStripMenuItem.Checked;
		}

		private void resetCameraPositionWhenModelIsLoadedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			resetCameraPositionWhenModelIsLoadedToolStripMenuItem.Checked = !resetCameraPositionWhenModelIsLoadedToolStripMenuItem.Checked;
			_ResetCameraOnModelLoad = resetCameraPositionWhenModelIsLoadedToolStripMenuItem.Checked;
		}

		private void reloadCurrentModelToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadResource();
		}

		private void interpolatePolygonColoursToolStripMenuItem_Click(object sender, EventArgs e)
		{
			interpolatePolygonColoursToolStripMenuItem.Checked = !interpolatePolygonColoursToolStripMenuItem.Checked;
			_ImportExportOptions.InterpolatePolygonColoursWhenColouringBasedOnVertices = interpolatePolygonColoursToolStripMenuItem.Checked;
			HandleRenderModeChange();
		}

		private void useEachUniqueTextureCLUTVariationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			useEachUniqueTextureCLUTVariationToolStripMenuItem.Checked = !useEachUniqueTextureCLUTVariationToolStripMenuItem.Checked;
			_ImportExportOptions.UseEachUniqueTextureCLUTVariation = useEachUniqueTextureCLUTVariationToolStripMenuItem.Checked;
			HandleRenderModeChange();
		}

		private void augmentAlphaMaskingFlagsBasedOnImageContentToolStripMenuItem_Click(object sender, EventArgs e)
		{
			augmentAlphaMaskingFlagsBasedOnImageContentToolStripMenuItem.Checked = !augmentAlphaMaskingFlagsBasedOnImageContentToolStripMenuItem.Checked;
			_ImportExportOptions.AlsoInferAlphaMaskingFromTexturePixels = augmentAlphaMaskingFlagsBasedOnImageContentToolStripMenuItem.Checked;
			HandleRenderModeChange();
		}

		private void ignorePolygonFlag2ForTerrainToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ignorePolygonFlag2ForTerrainToolStripMenuItem.Checked = !ignorePolygonFlag2ForTerrainToolStripMenuItem.Checked;
			_ImportExportOptions.IgnorePolygonFlag2ForTerrain = ignorePolygonFlag2ForTerrainToolStripMenuItem.Checked;
			HandleRenderModeChange();
		}

		private void createDistinctMaterialsForAllFlagsEvenIfUnusedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			createDistinctMaterialsForAllFlagsEvenIfUnusedToolStripMenuItem.Checked = !createDistinctMaterialsForAllFlagsEvenIfUnusedToolStripMenuItem.Checked;
			_ImportExportOptions.DistinctMaterialsForAllFlags = createDistinctMaterialsForAllFlagsEvenIfUnusedToolStripMenuItem.Checked;
		}

		private void adjustUVCoordinatesForBilinearFilteringToolStripMenuItem_Click(object sender, EventArgs e)
		{
			adjustUVCoordinatesForBilinearFilteringToolStripMenuItem.Checked = !adjustUVCoordinatesForBilinearFilteringToolStripMenuItem.Checked;
			_ImportExportOptions.AdjustUVs = adjustUVCoordinatesForBilinearFilteringToolStripMenuItem.Checked;
		}

		private void ignoreVertexColoursToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ignoreVertexColoursToolStripMenuItem.Checked = !ignoreVertexColoursToolStripMenuItem.Checked;
			_ImportExportOptions.IgnoreVertexColours = ignoreVertexColoursToolStripMenuItem.Checked;
		}

		protected void ResetPlatformDetection()
		{
			autodetectToolStripMenuItem.Checked = false;
			forcePlayStationToolStripMenuItem.Checked = false;
			forcePCToolStripMenuItem.Checked = false;
			forceDreamcastToolStripMenuItem.Checked = false;
			forcePlayStation2ToolStripMenuItem.Checked = false;
			forceXboxToolStripMenuItem.Checked = false;
		}

		private void autodetectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ResetPlatformDetection();
			autodetectToolStripMenuItem.Checked = true;
			_ImportExportOptions.ForcedPlatform = CDC.Platform.None;
		}

		private void forcePlayStationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ResetPlatformDetection();
			forcePlayStationToolStripMenuItem.Checked = true;
			_ImportExportOptions.ForcedPlatform = CDC.Platform.PSX;
		}

		private void forcePCToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ResetPlatformDetection();
			forcePCToolStripMenuItem.Checked = true;
			_ImportExportOptions.ForcedPlatform = CDC.Platform.PC;
		}

		private void forceDreamcastToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ResetPlatformDetection();
			forceDreamcastToolStripMenuItem.Checked = true;
			_ImportExportOptions.ForcedPlatform = CDC.Platform.Dreamcast;
		}

		private void forcePlayStation2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ResetPlatformDetection();
			forcePlayStation2ToolStripMenuItem.Checked = true;
			_ImportExportOptions.ForcedPlatform = CDC.Platform.PlayStation2;
		}

		private void forceXboxToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ResetPlatformDetection();
			forceXboxToolStripMenuItem.Checked = true;
			_ImportExportOptions.ForcedPlatform = CDC.Platform.Xbox;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void currentSceneCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			sceneControls.ResourceTree.Nodes.Clear();

			ComboBox comboBox = (ComboBox)sender;
			if (comboBox.SelectedItem == null)
			{
				return;
			}

			string selectedItem = comboBox.SelectedItem.ToString();
			RenderManager.Instance.SetCurrentScene(selectedItem);

			if (RenderManager.Instance.CurrentScene == null)
			{
				return;
			}

			Scene currentScene = (Scene)RenderManager.Instance.CurrentScene;

			TreeNode sceneTreeNode = new TreeNode("Scene");
			sceneTreeNode.Checked = true;

			foreach (Renderable renderable in currentScene.RenderInstances)
			{
				if (renderable is RenderInstance)
				{
					VisibilityNode objectNode = ((RenderInstance)renderable).Root;
					TreeNode objectTreeNode = new TreeNode(renderable.Name);
					objectTreeNode.Checked = objectNode.Visible;

					foreach (VisibilityNode modelNode in objectNode.Nodes)
					{
						TreeNode modelTreeNode = new TreeNode(modelNode.Name);
						modelTreeNode.Checked = modelNode.Visible;

						if (sceneTreeNode.Nodes.Count > 0)
						{
							continue;
						}

						foreach (VisibilityNode groupNode in modelNode.Nodes)
						{
							TreeNode groupTreeNode = new TreeNode(groupNode.Name);
							groupTreeNode.Checked = groupNode.Visible;
							modelTreeNode.Nodes.Add(groupTreeNode);
						}

						objectTreeNode.Nodes.Add(modelTreeNode);
					}

					sceneTreeNode.Nodes.Add(objectTreeNode);
				}
			}

			if (sceneTreeNode.Nodes.Count > 0)
			{
				sceneControls.ResourceTree.Nodes.Add(sceneTreeNode);
				sceneControls.ResourceTree.Nodes[0].Expand();
				sceneControls.ResourceTree.Nodes[0].Nodes[0].Expand();
			}
		}

		private void currentObjectCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			objectControls.ResourceTree.Nodes.Clear();

			ComboBox comboBox = (ComboBox)sender;
			if (comboBox.SelectedItem == null)
			{
				return;
			}

			string selectedItem = comboBox.SelectedItem.ToString();
			RenderManager.Instance.SetCurrentObject(selectedItem);

			if (RenderManager.Instance.CurrentObject == null)
			{
				return;
			}

			Scene currentObject = (Scene)RenderManager.Instance.CurrentObject;

			TreeNode objectSceneTreeNode = new TreeNode("Object");
			objectSceneTreeNode.Checked = true;

			foreach (Renderable renderable in currentObject.RenderInstances)
			{
				if (renderable is RenderInstance)
				{
					VisibilityNode objectNode = ((RenderInstance)renderable).Root;
					TreeNode objectTreeNode = new TreeNode(renderable.Name);
					objectTreeNode.Checked = objectNode.Visible;

					foreach (VisibilityNode modelNode in objectNode.Nodes)
					{
						TreeNode modelTreeNode = new TreeNode(modelNode.Name);
						modelTreeNode.Checked = modelNode.Visible;

						foreach (VisibilityNode groupNode in modelNode.Nodes)
						{
							TreeNode groupTreeNode = new TreeNode(groupNode.Name);
							groupTreeNode.Checked = groupNode.Visible;
							modelTreeNode.Nodes.Add(groupTreeNode);
						}

						objectTreeNode.Nodes.Add(modelTreeNode);
					}

					objectSceneTreeNode.Nodes.Add(objectTreeNode);
				}
			}

			if (objectSceneTreeNode.Nodes.Count > 0)
			{
				objectControls.ResourceTree.Nodes.Add(objectSceneTreeNode);
				objectControls.ResourceTree.Nodes[0].Expand();
			}
		}

		private void loadResourceButton_Click(object sender, EventArgs e)
		{
			if (SelectResourceToLoad(false, false))
			{
				LoadResource();
			}
		}

		private void unloadResourceButton_Click(object sender, EventArgs e)
		{
			if (resourceList.SelectedItems.Count > 0)
			{
				string selectedItem = resourceList.SelectedItems[0].Text;

				RenderManager.Instance.UnloadResource(selectedItem);

				if (objectControls.ResourceCombo.SelectedItem?.ToString() == selectedItem)
				{
					objectControls.ResourceCombo.SelectedIndex = -1;
				}
				objectControls.ResourceCombo.Items.Remove(selectedItem);

				if (sceneControls.ResourceCombo.SelectedItem?.ToString() == selectedItem)
				{
					sceneControls.ResourceCombo.SelectedIndex = -1;
				}
				sceneControls.ResourceCombo.Items.Remove(selectedItem);

				resourceList.Items.Remove(resourceList.SelectedItems[0]);
			}
		}

		private void loadObjectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectResourceToLoad(true, false))
			{
				LoadResource();
			}
		}

		private void loadSceneToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectResourceToLoad(false, true))
			{
				LoadResource();
			}
		}

		private void optionTabs_SelectedIndexChanged(object sender, EventArgs e)
		{
		}

		private void unloadAllResources_Click(object sender, EventArgs e)
		{
			RenderManager.Instance.UnloadResources();

			objectControls.ResourceCombo.SelectedIndex = -1;
			sceneControls.ResourceCombo.SelectedIndex = -1;

			resourceList.Items.Clear();
			objectControls.ResourceCombo.Items.Clear();
			sceneControls.ResourceCombo.Items.Clear();
		}

        private void sceneToolStripMenuItem_Click(object sender, EventArgs e)
        {
			SceneMode = 0;
		}

        private void objectToolStripMenuItem_Click(object sender, EventArgs e)
        {
			SceneMode = 1;
		}
    }
}