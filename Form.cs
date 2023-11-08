using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FluidSimulationJosStam
{
	public partial class Form : System.Windows.Forms.Form
	{
		// https://www.dgp.toronto.edu/public_user/stam/reality/Research/pdf/GDC03.pdf

		public DrawPanel _DrawPanel;
		private Fluid f;
		Random m_Random = new Random();
		public Form()
		{
			InitializeComponent();

			Width = fa.NX * fa.displayScale;
			Height = fa.NY * fa.displayScale;
			Text = "Fluid simulations by Jos Stam ported to c# by Jens Malmgren";

			_DrawPanel = new DrawPanel();
			_DrawPanel.Dock = DockStyle.Fill;
			Controls.Add(_DrawPanel);

			f = new Fluid();

			_DrawPanel.Paint += OnDrawPanel_Paint;
		}

		private void OnDrawPanel_Paint(object sender, PaintEventArgs e)
		{
			float _fDensity = 1f;
			float _fVelocity = 400f;

			fa.angle += m_Random.Next(-1, 2);
			
			if (fa.angle > 360)
			{
				fa.angle = fa.angle - 360;
			}
			else
			{
				if (fa.angle < 0)
				{
					fa.angle = fa.angle + 360;
				}
			}

			float _xDir = _fVelocity * (float)Math.Sin((float)fa.angle / Math.PI * 2f);
			float _yDir = _fVelocity * (float)Math.Cos((float)fa.angle / Math.PI * 2f);
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					f.dens[f.IX(i + fa.NX / 2, j + fa.NY / 2)] += _fDensity;
					f.add_velocity(i + fa.NX / 2, j + fa.NY / 2, _xDir, _yDir);
				}
			}

			for (int x = 0; x < fa.NX; x++)
			{
				for (int y = 0; y < fa.NY; y++)
				{
					float _density = f.dens[f.IX(x, y)];
					float _ratioWithMaxDensity = _density / _fDensity;
					int _c = fa.Clamp((int)(_ratioWithMaxDensity * 255f) , 0, 255);
					Color _Color = Color.FromArgb(_c, _c, _c);
					using (SolidBrush _Solid = new SolidBrush(_Color))
					{
						e.Graphics.FillRectangle(_Solid, x * fa.displayScale, y * fa.displayScale, fa.displayScale, fa.displayScale);
					}
				}
			}

			f.simulate();
			_DrawPanel.Invalidate();
		}
	}

	/// <summary>
	/// "Fluid All" global variables.
	/// </summary>
	public static class fa
	{
		public static int NX = 100;
		public static int NY = 100;
		public static int iter = 4;
		public static int displayScale = 6;
		public static int angle = 0;
		public static float dt = 0.01f;
		public static float diff = 0.0001f;

		public static int Clamp(int value, int min, int max)
		{
			return (value < min) ? min : (value > max) ? max : value;
		}
	}

	public enum Boundary
	{
		None = 0,
		LeftAndRight = 1,
		TopAndBottom = 2
	}

	public class Fluid
	{
		/// <summary>
		/// Previous density
		/// </summary>
		public float[] dens_prev;
		/// <summary>
		/// Density
		/// </summary>
		public float[] dens;
		/// <summary>
		/// Horizontal velocity
		/// </summary>
		public float[] u;
		/// <summary>
		/// Vertical velocity
		/// </summary>
		public float[] v;
		/// <summary>
		/// Previous horizontal velocity
		/// </summary>
		public float[] u_prev;
		/// <summary>
		/// Previous vertical velocity
		/// </summary>
		public float[] v_prev;

		public Fluid()
		{
			int faNXExtra = fa.NX + 2;
			int faNYExtra = fa.NY + 2;
			this.dens_prev = new float[faNXExtra * faNYExtra];
			this.dens = new float[faNXExtra * faNYExtra];

			this.u = new float[faNXExtra * faNYExtra];
			this.v = new float[faNXExtra * faNYExtra];

			this.u_prev = new float[faNXExtra * faNYExtra];
			this.v_prev = new float[faNXExtra * faNYExtra];
		}

		public void simulate()
		{
			vel_step(u, v, u_prev, v_prev);
			dens_step(dens, dens_prev, u, v);
		}

		public int IX(int x, int y)
		{
			return x + y * (fa.NX + 2);
		}

		public void add_velocity(int x, int y, float amountX, float amountY)
		{
			int index = IX(x, y);

			u[index] += fa.dt * amountX;
			v[index] += fa.dt * amountY;
		}

		public void vel_step(float[] u, float[] v, float[] u0, float[] v0)
		{
			add_source(u, u0);
			add_source(v, v0);

			SWAP(ref u0, ref u); diffuse(Boundary.LeftAndRight, u, u0);
			SWAP(ref v0, ref v); diffuse(Boundary.TopAndBottom, v, v0);

			project(u, v, u0, v0);

			SWAP(ref u0, ref u);
			SWAP(ref v0, ref v);

			advect(Boundary.LeftAndRight, u, u0, u0, v0);
			advect(Boundary.TopAndBottom, v, v0, u0, v0);

			project(u, v, u0, v0);
		}

		public void diffuse(Boundary b, float[] x, float[] x0)
		{
			float a = fa.dt * fa.diff * (fa.NX - 2) * (fa.NY - 2);
			for (int k = 0; k < fa.iter; k++)
			{
				for (int i = 1; i <= fa.NX; i++)
				{
					for (int j = 1; j <= fa.NY; j++)
					{
						x[IX(i, j)] = (x0[IX(i, j)] + a * (x[IX(i - 1, j)] + x[IX(i + 1, j)]
														 + x[IX(i, j - 1)] + x[IX(i, j + 1)])) / (1 + 6*a);
					}
				}
				set_bnd(b, x);
			}

		} // diffuse()

		public void advect(Boundary b, float[] d, float[] d0, float[] u, float[] v)
		{
			int i, j, i0, j0, i1, j1;
			float x, y, s0, t0, s1, t1, dt0x, dt0y;

			dt0x = fa.dt * fa.NX;
			dt0y = fa.dt * fa.NY;

			for (i = 1; i <= fa.NX; i++)
			{
				for (j = 1; j <= fa.NY; j++)
				{
					x = (float)i - dt0x * u[IX(i, j)];
					y = (float)j - dt0y * v[IX(i, j)];

					if (x < 0.5f) x = 0.5f;
					if (x > (float)fa.NX + 0.5f) x = (float)fa.NX + 0.5f;
					i0=(int)x;
					i1 = i0 + 1;

					if (y < 0.5f) y = 0.5f;
					if (y > (float)fa.NY + 0.5f) y = (float)fa.NY + 0.5f;
					j0 = (int)y;
					j1 = j0 + 1;

					s1 = x - i0;
					s0 = 1.0f - s1;
					t1 = y - j0;
					t0 = 1.0f - t1;

					d[IX(i, j)] = s0 * (t0 * d0[IX(i0, j0)] + t1 * d0[IX(i0, j1)]) +
								  s1 * (t0 * d0[IX(i1, j0)] + t1 * d0[IX(i1, j1)]);
				}
			}
			set_bnd(b, d);
		}

		public void add_source(float[] x, float[] s)
		{
			int size = (fa.NX + 2) * (fa.NY + 2);
			for (int i = 0; i < size; i++)
			{
				x[i] += fa.dt * s[i];
			}
		}

		void SWAP(ref float[] x0, ref float[] x)
		{
			float[] tmp = x0;
			x0 = x;
			x = tmp;
		}

		void dens_step(float[] x, float[] x0, float[] u, float[] v)
		{
			add_source(x, x0);
			SWAP(ref x0, ref x); diffuse(0, x, x0);
			SWAP(ref x0, ref x); advect(0, x, x0, u, v);
		}

		public void project(float[] u, float[] v, float[] p, float[] div)
		{
			float hx = 1.0f / fa.NX;
			float hy = 1.0f / fa.NY;

			for (int i = 1; i <= fa.NX; i++)
			{
				for (int j = 1; j <= fa.NY; j++)
				{
					div[IX(i, j)] = -0.5f * hx * (u[IX(i + 1, j)] - u[IX(i - 1, j)]	+
												  v[IX(i, j + 1)] - v[IX(i, j - 1)]);
					p[IX(i, j)] = 0;
				}
			}
			set_bnd(0, div);
			set_bnd(0, p);

			for (int k = 0; k < fa.iter; k++)
			{
				for (int i = 1; i <= fa.NX; i++)
				{
					for (int j = 1; j <= fa.NY; j++)
					{
						p[IX(i, j)] = (div[IX(i, j)] + p[IX(i - 1, j)] + p[IX(i + 1, j)] +
						                               p[IX(i, j - 1)] + p[IX(i, j + 1)]) / 4;
					}
				}
				set_bnd(0, p);
			}

			for (int i = 1; i <= fa.NX; i++)
			{
				for (int j = 1; j <= fa.NY; j++)
				{
					u[IX(i, j)] -= 0.5f * (p[IX(i + 1, j    )] - p[IX(i - 1, j    )]) / hx;
					v[IX(i, j)] -= 0.5f * (p[IX(i    , j + 1)] - p[IX(i    , j - 1)]) / hy;
				}
			}
			set_bnd(Boundary.LeftAndRight, u);
			set_bnd(Boundary.TopAndBottom, v);
		} // project()

		void set_bnd(Boundary b, float[] x)
		{
			for (int i = 1; i < fa.NY - 1; i++)
			{
				x[IX(0, i)] =         b == Boundary.LeftAndRight ? -x[IX(1, i)] : x[IX(1, i)];
				x[IX(fa.NX + 1, i)] = b == Boundary.LeftAndRight ? -x[IX(fa.NX, i)] : x[IX(fa.NX, i)];
			}

			for (int i = 1; i < fa.NX - 1; i++)
			{
				x[IX(i, 0        )] = b == Boundary.TopAndBottom ? -x[IX(i, 1        )] : x[IX(i, 1)];
				x[IX(i, fa.NY + 1)] = b == Boundary.TopAndBottom ? -x[IX(i, fa.NY)] : x[IX(i, fa.NY)];
			}

			x[IX(0        , 0        )] = 0.5f * (x[IX(1        , 0        )] + x[IX(0        , 1        )]);
			x[IX(0        , fa.NY + 1)] = 0.5f * (x[IX(1        , fa.NY + 1)] + x[IX(0        , fa.NY    )]);
			x[IX(fa.NX + 1, 0        )] = 0.5f * (x[IX(fa.NX    , 0        )] + x[IX(fa.NX + 1, 1        )]);
			x[IX(fa.NX + 1, fa.NY + 1)] = 0.5f * (x[IX(fa.NX    , fa.NY + 1)] + x[IX(fa.NX + 1, fa.NY    )]);
		} // set_bnd()

	}

	public class DrawPanel : Panel
	{
		public DrawPanel()
		{
			DoubleBuffered = true;
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
		}
	}
}