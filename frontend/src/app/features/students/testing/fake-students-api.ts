import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FakeResponder, notFound } from '../../../testing/fake-responder';
import { PublicApi } from '../../../testing/public-api';
import { StudentRequest, StudentResponse } from '../student.model';
import { StudentsApi } from '../students-api';

export type StudentsCall = 'list' | 'get' | 'create' | 'update' | 'remove';

export const HAS_LESSON_PLANS =
  'This student has lesson plans. Delete them first or move them to another student.';

export function aStudent(overrides: Partial<StudentResponse> = {}): StudentResponse {
  return {
    id: 's1',
    name: 'Ana',
    email: null,
    englishLevel: 'Intermediate',
    notes: null,
    lessonPlanCount: 0,
    ...overrides,
  };
}

export class FakeStudentsApi implements PublicApi<StudentsApi> {
  readonly responder = new FakeResponder<StudentsCall>();
  students: StudentResponse[] = [];
  private nextId = 1;

  list(search: string): Observable<StudentResponse[]> {
    const term = search.trim().toLowerCase();
    return this.responder.respond('list', () =>
      this.students.filter((student) => student.name.toLowerCase().includes(term)),
    );
  }

  get(id: string): Observable<StudentResponse> {
    return this.responder.respond('get', () => this.find(id));
  }

  create(request: StudentRequest): Observable<StudentResponse> {
    return this.responder.respond('create', () => {
      const student = aStudent({ ...request, id: `new-${this.nextId++}` });
      this.students = [...this.students, student];
      return student;
    });
  }

  update(id: string, request: StudentRequest): Observable<StudentResponse> {
    return this.responder.respond('update', () => {
      const student = { ...this.find(id), ...request };
      this.students = this.students.map((current) => (current.id === id ? student : current));
      return student;
    });
  }

  remove(id: string): Observable<void> {
    return this.responder.respond('remove', () => {
      if (this.find(id).lessonPlanCount > 0) {
        throw new HttpErrorResponse({ status: 409, error: { detail: HAS_LESSON_PLANS } });
      }
      this.students = this.students.filter((student) => student.id !== id);
    });
  }

  private find(id: string): StudentResponse {
    const student = this.students.find((current) => current.id === id);
    if (student === undefined) {
      throw notFound();
    }
    return student;
  }
}
