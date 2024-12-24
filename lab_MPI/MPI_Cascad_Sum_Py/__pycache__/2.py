from mpi4py import MPI

comm = MPI.COMM_WORLD
size = comm.Get_size()
worker = comm.Get_rank()

print("Hello world", worker, 'of', size)